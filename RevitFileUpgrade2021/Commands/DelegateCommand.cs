using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RevitFileUpgrade.Commands
{
    class DelegateCommand : ICommand
    {
        protected Action<object> DelegateExecute { get; }
        protected Func<object, bool> DelegateCanExecute { get; }

        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            DelegateExecute = execute;
            DelegateCanExecute = canExecute;
        }

    
        public event EventHandler CanExecuteChanged;

        public virtual bool CanExecute(object parameter)
        {
            bool result;
            try
            {
                var delegateCanExecute = DelegateCanExecute;
                result = delegateCanExecute == null || delegateCanExecute(parameter);
            }
            catch (Exception o)
            {
                result = false;
            }

            return result;
        }


        public virtual void Execute(object parameter)
        {
            try
            {
                var delegateExecute = DelegateExecute;
                delegateExecute?.Invoke(parameter);
            }
            catch (Exception o)
            {
            }
        }
    }
}
