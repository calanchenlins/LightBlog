using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.VSTool.ToolWindows
{
    public class ReplaceViewModel //: INotifyPropertyChanged
    {
        public string FindTarget { get; set; }

        public string ReplaceWith { get; set; }

        //private bool _adjustNamespaces = false;

        public bool AdjustNamespaces { get; set; }
        //public bool AdjustNamespaces
        //{
        //    get => _adjustNamespaces;
        //    set
        //    {
        //        if (value == _adjustNamespaces) return;
        //        _adjustNamespaces = value;
        //        OnPropertyChanged();
        //    }
        //}

        public string FileEncoding { get; set; } = "utf-8";

        public IReadOnlyList<string> GenerationModeList { get; set; } = new[]
            {
                "utf-8",
                "utf-16",
                "utf-16BE",
                "utf-32",
                "us-ascii",
            };






        //public event PropertyChangedEventHandler PropertyChanged;

        //private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}
