using System;

namespace LcmsSpectator.DialogServices
{
    public interface IDialogService
    {
        string OpenFile(string defaultExt, string filter);
        bool ConfirmationBox(string message, string title);
        void MessageBox(string text);
        void ExceptionAlert(Exception e);
    }
}
