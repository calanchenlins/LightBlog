using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Dashboard.Management.Views.Shared
{
    public class CustomLayoutPage : Hangfire.Dashboard.Pages.LayoutPage
    {
        public CustomLayoutPage(string title) : base(title)
        {
        }

        public override void Execute()
        {
            //this.
            base.Execute();
            //this.Layout.
        }

        public override string ToString()
        {
            //var _content = base.ToString().AsSpan();
            //_content.
            return base.ToString();
        }

        protected override object RenderBody()
        {
            return base.RenderBody();
        }

        protected override void Write(object value)
        {
            base.Write(value);
        }
    }
}
