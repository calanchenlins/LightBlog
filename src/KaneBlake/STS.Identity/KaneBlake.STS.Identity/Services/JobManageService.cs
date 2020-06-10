using AspectCore.Extensions.Reflection;
using CoreWeb.Util.Infrastruct;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using KaneBlake.Basis.Extensions.Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public class JobManageService : IJobManageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly JobEntryResolver _jobEntryResolver;

        public JobManageService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, IRecurringJobManager recurringJobManager, JobEntryResolver jobEntryResolver)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _jobEntryResolver = jobEntryResolver ?? throw new ArgumentNullException(nameof(jobEntryResolver));
        }

        public async Task RecurringJobAddOrUpdateAsync(string id,string targetTypeName, string targetMethodName,string cronExpression, TimeZoneInfo timeZone = null,string queue = EnqueuedState.DefaultQueue)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return;
            }
            var jobEntry = _jobEntryResolver.GetJobEntry(targetTypeName, targetMethodName);
            if (jobEntry == null) 
            {
                return;
            }
            var form = await context.Request.ReadFormAsync();
            Expression instance = null;
            if (!jobEntry.IsStaticMethod)
            {
                // 获取对象实例
                var TargetInstance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, jobEntry.TargetType);
                instance = Expression.Constant(TargetInstance, jobEntry.TargetType);
            }
            var job = jobEntry.GetJob(instance, form);
            if (job != null) 
            {
                _recurringJobManager.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
            }

            // lambda表达式参数取自容器
            // ()=> action();
            // (T)=> action(T,OP1,OP2)
            // ()=> func(OP1,OP2); 返回Task
            // (T)=> func(T,OP1,OP2); 返回Task

        }

        public string GetAllJobEntries() => _jobEntryResolver.GetAllJobEntries();
    }

    public class JobEntryResolver 
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private static readonly ConcurrentDictionary<string, List<JobEntry>> JobEntryCache = new ConcurrentDictionary<string, List<JobEntry>>();

        public JobEntryResolver(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger(typeof(JobEntryResolver));
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            LoadJobEntry(assemblies);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        public JobEntry GetJobEntry(string typeName,string methodName) 
        {
            if (JobEntryCache.TryGetValue(typeName, out var jobEntries))
            {
                return jobEntries.FirstOrDefault(jobEntry => jobEntry.TargetMethodName.Equals(methodName));
            }
            else 
            {
                try
                {
                    var targetType = Type.GetType(typeName);
                    var targetMethod = targetType.GetMethod(methodName);
                    var jobEntry = new JobEntry(targetType, targetMethod);
                    return jobEntry;
                }
                catch (Exception) 
                {
                    return null;
                }
            }
        }

        public string GetAllJobEntries() 
        {
            var _ = JobEntryCache.Select(job => job.Value.Select(r => new { typeName = job.Key, methodName = r.TargetMethodName, ParameterNames = r.GetParameters() })).ToList();
            return Newtonsoft.Json.JsonConvert.SerializeObject(_);
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            LoadJobEntry(new List<Assembly>() { args.LoadedAssembly });
        }
        private void LoadJobEntry(List<Assembly> assemblies)
        {
            assemblies.ForEach(assembliy => {
                try
                {
                    var jobTemplateAttributes = assembliy.GetCustomAttributes<JobTemplateAttribute>().ToList();
                    var tt =jobTemplateAttributes.Select(jobTemplateAttribute => new 
                    { 
                        typename = jobTemplateAttribute.TargetType.AssemblyQualifiedName, 
                        jobEntries= jobTemplateAttribute.TargetType.GetMethods().Where(m => m.GetCustomAttributes<JobTemplateAttribute>().Any()).Select(method => new JobEntry(jobTemplateAttribute.TargetType, method))
                    });
                    if (assembliy.FullName.Contains("KaneBlake.Basis")) {
                        var ttg = "";
                    }
                    foreach (var jobTemplateAttribute in jobTemplateAttributes) 
                    {
                        var methods = jobTemplateAttribute.TargetType.GetMethods().Where(m => m.GetCustomAttributes<JobTemplateAttribute>().Any()).ToList();
                        var jobEntries =methods.Select(method => new JobEntry(jobTemplateAttribute.TargetType, method)).ToList();
                        JobEntryCache.TryAdd(jobTemplateAttribute.TargetType.AssemblyQualifiedName, jobEntries.ToList());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while load JobEntry From Assembly:'{0}'", assembliy.FullName);
                }
            });
        }

    }

    public class JobEntry
    {
        public Type TargetType { get; private set; }
        private readonly MethodInfo _targetMethod;
        private readonly MethodReflector _reflector;
        public bool IsStaticMethod { get => _targetMethod.IsStatic; }
        public string TargetMethodName { get => _targetMethod.Name; }

        public JobEntry(Type targetType, MethodInfo targetMethod)
        {
            _targetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            _reflector = targetMethod.GetReflector();
        }

        public IEnumerable<string> GetParameters() => _reflector.ParameterReflectors.Select(p => p.Name);
        public Job GetJob(Expression instance, IFormCollection form) 
        {
            var parameters = _reflector.ParameterReflectors;
            var constantExpressions = new List<ConstantExpression>(parameters.Length);
            foreach (var parameterInfo in parameters)
            {
                var _ = form.FirstOrDefault(el => parameterInfo.Name.Equals(el.Key, StringComparison.OrdinalIgnoreCase));
                // TypeConverter???
                ConstantExpression constantExpression = null;
                if (!_.Value.Equals(StringValues.Empty))
                {
                    constantExpression = Expression.Constant(_.Value.FirstOrDefault(), parameterInfo.ParameterType);
                }
                else if (parameterInfo.HasDeflautValue)
                {
                    constantExpression = Expression.Constant(parameterInfo.DefalutValue, parameterInfo.ParameterType);
                }
                else
                {
                    constantExpression = Expression.Constant(null, parameterInfo.ParameterType);
                }
                constantExpressions.Add(constantExpression);
            }
            var methodCallExpression = Expression.Call(instance, _targetMethod, constantExpressions.ToArray());
            Job job = null;
            // void action();
            if (_targetMethod.ReturnType != typeof(Task) && _reflector.GetCustomAttribute<AsyncStateMachineAttribute>() == null)
            {
                //_targetMethod.ReturnType == typeof(void) && 
                var methodCall = Expression.Lambda<Action>(methodCallExpression, new ParameterExpression[] { });
                job = Job.FromExpression(methodCall);
            }
            // Task func();
            // async Task func();
            else if (_targetMethod.ReturnType == typeof(Task))
            {
                var methodCall = Expression.Lambda<Func<Task>>(methodCallExpression, new ParameterExpression[] { });
                job = Job.FromExpression(methodCall);
            }
            return job;
        }
    }

}
