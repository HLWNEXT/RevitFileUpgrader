using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RevitFileUpgrade.Interfaces;

namespace RevitFileUpgrade.ViewModels
{
    class BaseViewModel : ObservableObject, IViewModel
    {
        public string Id { get; }

        public string Name => GetType().Name;

        public void TryInvoke(Action action)
        {
        }
    }
}
