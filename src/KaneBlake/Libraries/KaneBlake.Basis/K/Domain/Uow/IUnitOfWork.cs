using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace K.Domain.Uow
{
    /// <summary>
    /// 工作单元接口
    /// 仓储继承IUnitOfWork
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        void Complete();

        /// <summary>
        /// 工作单元提交
        /// </summary>
        Task<int> CompleteAsync();
    }
}
