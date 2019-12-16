using CoreWeb.Util.Infrastruct;
using KaneBlake.Basis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace KaneBlake.Basis.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void IsPrime_InputIs1_ReturnFalse()
        {
            //var result = new DbKernalHelp();
            //var efc = result.ExecuteTSqlInTran("Server=101.132.150.163,57803;Database=LightBlogDb;User Id=sa;Password=b6a88a661724bdd4d97a4d3513de7f6", 
            //    $@"insert INTO dbo.[User] values(3,'23',GETDATE(),'23')
            //    insert INTO dbo.[User] values(3,'23',GETDATE(),'23')",
            //    new List<Microsoft.Data.SqlClient.SqlParameter>());
            //Assert.False(efc==0, "1 should not be prime");
        }
    }
}
