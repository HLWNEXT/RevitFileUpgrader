using System;

namespace RevitFileUpgrade.Interfaces
{
    public interface IViewModel
    {
        string Id { get; }
        string Name { get; }
        void TryInvoke(Action action);
    }
}
