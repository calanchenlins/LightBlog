using AspectCore.Extensions.Reflection;
using CoreWeb.Util.Infrastruct;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using KaneBlake.Basis.Common.Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public class JobManageService : IJobManageService
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly JobStorage _jobStorage;
        private readonly JobEntryResolver _jobEntryResolver;

        public JobManageService(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, IRecurringJobManager recurringJobManager, JobEntryResolver jobEntryResolver, IBackgroundJobClient backgroundJobClient, JobStorage jobStorage)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger(typeof(JobEntryResolver));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _jobEntryResolver = jobEntryResolver ?? throw new ArgumentNullException(nameof(jobEntryResolver));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _jobStorage = jobStorage ?? throw new ArgumentNullException(nameof(jobStorage));
        }

        private async Task<Job> GetJobAsync(string targetTypeName, string targetMethodName)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return null;
            }
            var jobEntry = _jobEntryResolver.GetJobEntry(targetTypeName, targetMethodName);
            if (jobEntry == null)
            {
                return null;
            }
            FormCollection form = FormCollection.Empty;
            MediaType parsedContentType = new MediaType(context.Request.ContentType);
            if (parsedContentType.IsSubsetOf(_jobEntryResolver.SupportedMediaType))
            {
                context.Request.Body.Position = 0;
                var body = await context.Request.BodyReader.ReadAsync();
                var jsonString = Encoding.UTF8.GetString(body.Buffer.ToArray());
                var dict = new Dictionary<string, StringValues>();
                using (JsonDocument document = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind.Equals(JsonValueKind.Object))
                    {
                        foreach (JsonProperty jsonProperty in root.EnumerateObject())
                        {
                            dict.Add(jsonProperty.Name, new StringValues(jsonProperty.Value.ToString()));
                        }
                    }
                }
                form = new FormCollection(dict);
            }
            Expression instance = null;
            if (!jobEntry.IsStaticMethod)
            {
                // 获取对象实例
                var TargetInstance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, jobEntry.TargetType);
                instance = Expression.Constant(TargetInstance, jobEntry.TargetType);
            }
            var job = jobEntry.GetJob(instance, form);
            return job;
        }

        public async Task RecurringJobAddOrUpdateAsync(RecurringJobInDto recurringJobInDto)
        {
            recurringJobInDto.Id = string.IsNullOrEmpty(recurringJobInDto.Id) ? Guid.NewGuid().ToString() : recurringJobInDto.Id;
            TimeZoneInfo timeZoneInfo;
            if (!string.IsNullOrEmpty(recurringJobInDto.TimeZoneId))
            {
                try
                {
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(recurringJobInDto.TimeZoneId);
                }
                catch
                {
                    timeZoneInfo = TimeZoneInfo.Utc;
                }
            }
            else 
            {
                timeZoneInfo = TimeZoneInfo.Utc;
            }
            recurringJobInDto.Queue = string.IsNullOrEmpty(recurringJobInDto.Queue) ? EnqueuedState.DefaultQueue : recurringJobInDto.Queue;
            var job = await GetJobAsync(recurringJobInDto.TypeName, recurringJobInDto.MethodName);
            if (job != null)
            {
                _recurringJobManager.AddOrUpdate(recurringJobInDto.Id, job, recurringJobInDto.Cron, timeZoneInfo, recurringJobInDto.Queue);
            }
            // lambda表达式参数取自容器
            // ()=> action();
            // (T)=> action(T,OP1,OP2)
            // ()=> func(OP1,OP2); 返回Task
            // (T)=> func(T,OP1,OP2); 返回Task
        }

        public async Task BackgroundJobCreateAsync(string targetTypeName, string targetMethodName, string queue = EnqueuedState.DefaultQueue)
        {
            var job = await GetJobAsync(targetTypeName, targetMethodName);
            if (job == null)
            {
                return;
            }
            _backgroundJobClient.Create(job, new EnqueuedState(queue));
        }
        public async Task BackgroundJobCreateAsync(string targetTypeName, string targetMethodName, DateTime enqueueAt)
        {
            var job = await GetJobAsync(targetTypeName, targetMethodName);
            if (job == null)
            {
                return;
            }
            if (enqueueAt == default)
            {
                _backgroundJobClient.Create(job, new ScheduledState(TimeSpan.FromMinutes(10)));
            }
            _backgroundJobClient.Create(job, new ScheduledState(enqueueAt));
        }

        public IEnumerable<JobEntryViewModel> GetAllJobEntries()
        {
            var all = _jobEntryResolver.GetAllJobEntries();
            foreach (var job in all)
            {
                yield return new JobEntryViewModel()
                {
                    TypeName = job.TargetTypeName,
                    MethodName = job.TargetMethodName,
                    Parameters = job.GetParameterss()

                };
            }
        }

        public RecurringJobViewModel GetRecurringJobById(string id)
        {
            using var connection = _jobStorage.GetConnection();
            var hash = connection.GetAllEntriesFromHash($"recurring-job:{id}");
            if (hash == null)
            {
                return null;
            }
            var _parameters = new List<KeyValuePair<string, string>>
                {
                    KeyValuePair.Create(nameof(RecurringJobViewModel.Cron), hash["Cron"])
                };
            var model = new RecurringJobViewModel
            {
                Id = id,
                Cron = hash["Cron"],
                Parameters = _parameters
            };

            try
            {
                if (hash.TryGetValue("Job", out var payload) && !String.IsNullOrWhiteSpace(payload))
                {
                    var invocationData = InvocationData.DeserializePayload(payload);
                    var Job = invocationData.DeserializeJob();
                    model.TypeName = invocationData.Type;
                    model.MethodName = invocationData.Method;
                    ParameterInfo[] parameters = Job.Method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        _parameters.Add(KeyValuePair.Create(parameters[i].Name, Job.Args[i]?.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load the job:{0}", id);
            }
            if (hash.ContainsKey("Queue"))
            {
                model.Queue = hash["Queue"];
                _parameters.Add(KeyValuePair.Create(nameof(model.Queue), model.Queue));
            }
            if (hash.ContainsKey("TimeZoneId"))
            {
                model.TimeZoneId = hash["TimeZoneId"];
                _parameters.Add(KeyValuePair.Create(nameof(model.TimeZoneId), model.TimeZoneId));
            }
            return model;
        }
    }

    public class JobEntryResolver 
    {
        public MediaType SupportedMediaType { get; } = new MediaType("application/json");
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

        public IEnumerable<JobEntry> GetAllJobEntries() 
        {
            var all = JobEntryCache.Select(j => j.Value);
            foreach (var jobs in all) 
            {
                foreach (var job in jobs)
                {
                    yield return job;
                }
            }
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

    public class JobEntryInDto
    {
        public string TypeName { get; set; }
        public string MethodName { get; set; }
    }

    public class JobEntryViewModel : JobEntryInDto
    {
        public IEnumerable<KeyValuePair<string, string>> Parameters { get; set; }
    }

    public class RecurringJobInDto: JobEntryInDto
    {
        public string Id { get; set; }
        public string Cron { get; set; }
        public string Queue { get; set; }
        public string TimeZoneId { get; set; }
    }
    public class RecurringJobViewModel : RecurringJobInDto
    {
        public IEnumerable<KeyValuePair<string, string>> Parameters { get; set; }
    }

    public class JobEntry
    {
        public Type TargetType { get; private set; }

        private readonly MethodInfo _targetMethod;

        private readonly MethodReflector _reflector;

        public bool IsStaticMethod { get => _targetMethod.IsStatic; }

        public string TargetTypeName { get => TargetType.AssemblyQualifiedName; }

        public string TargetMethodName { get => _targetMethod.Name; }

        public JobEntry(Type targetType, MethodInfo targetMethod)
        {
            _targetMethod = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            _reflector = targetMethod.GetReflector();
        }

        public IEnumerable<string> GetParameters() => _reflector.ParameterReflectors.Select(p => p.Name);

        public IEnumerable<KeyValuePair<string, string>> GetParameterss() 
        {
            return _reflector.ParameterReflectors.Select(p => KeyValuePair.Create(p.Name,p.HasDeflautValue? p.DefalutValue?.ToString()?? string.Empty : string.Empty));
        }
        public Job GetJob(Expression instance, IFormCollection form) 
        {
            var parameters = _reflector.ParameterReflectors;
            var constantExpressions = new List<ConstantExpression>(parameters.Length);
            foreach (var parameterInfo in parameters)
            {
                var jsonPropertyValue = form.FirstOrDefault(el => parameterInfo.Name.Equals(el.Key, StringComparison.OrdinalIgnoreCase)).Value;
                object value = null;
                if (!jsonPropertyValue.Equals(StringValues.Empty))
                {
                    // SimpleType
                    if (TypeDescriptor.GetConverter(parameterInfo.ParameterType).CanConvertFrom(typeof(string)))
                    {
                        var _typeConverter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);
                        value = _typeConverter.ConvertFrom(jsonPropertyValue.FirstOrDefault());
                    }
                    // ComplexType
                    else
                    {
                        value = JsonSerializer.Deserialize(jsonPropertyValue.FirstOrDefault(),parameterInfo.ParameterType);
                    }

                }
                else if (parameterInfo.HasDeflautValue)
                {
                    value = parameterInfo.DefalutValue;
                }
                else
                {
                    value = parameterInfo.ParameterType.GetDefaultValue();
                }
                var constantExpression = Expression.Constant(value, parameterInfo.ParameterType);
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


    internal interface IScopedProcessingService
    {
        Task DoWork(CancellationToken stoppingToken);
    }

    internal class ScopedProcessingService : IScopedProcessingService
    {
        private int executionCount = 0;
        private readonly ILogger _logger;

        public ScopedProcessingService(ILogger<ScopedProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    // 模拟后台任务
                    Thread.Sleep(1000000);


                    // 防止后台任务线程持续抢占CPU
                    Thread.Sleep(1);
                }
            }, stoppingToken);


            while (!stoppingToken.IsCancellationRequested)
            {

                await Task.Run(() =>
                {
                    // 模拟后台任务
                    executionCount++;
                    _logger.LogInformation("Scoped Processing Service is working. Count: {Count}", executionCount);
                    Thread.Sleep(1000000);

                },stoppingToken);

            }
        }
    }
    public class ConsumeScopedServiceHostedService : BackgroundService
    {
        private readonly ILogger<ConsumeScopedServiceHostedService> _logger;

        public ConsumeScopedServiceHostedService(IServiceProvider services,
            ILogger<ConsumeScopedServiceHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service running.");

            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IScopedProcessingService>();

                await scopedProcessingService.DoWork(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await Task.CompletedTask;
        }
    }

}
