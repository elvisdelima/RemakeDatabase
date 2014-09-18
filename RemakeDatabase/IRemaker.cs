using System;

namespace RemakeDatabase
{
    public interface IRemaker
    {
        event Action<string> ReportProcess;
        event Action<int, int, int> ReportScriptExecuting;
        void Remake();
    }
}