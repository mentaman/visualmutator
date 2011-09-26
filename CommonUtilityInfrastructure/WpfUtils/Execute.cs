﻿namespace CommonUtilityInfrastructure.WpfUtils
{
    #region Usings

    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;

    #endregion

    public interface IExecute
    {
        void OnUIThread(Action action);

        TaskScheduler GuiScheduler { get; }
    }

    public class Execute : IExecute
    {
        private Action<Action> _executor = action => action();

        private TaskScheduler _guiScheduler;

        public void OnUIThread(Action action)
        {
            _executor(action);
        }

        public TaskScheduler GuiScheduler
        {
            get
            {
                return _guiScheduler;
            }
        }

        public void InitializeWithDispatcher()
        {
            Dispatcher dispatcher = Application.Current.Dispatcher;
            _guiScheduler = TaskScheduler.FromCurrentSynchronizationContext();


            _executor = action =>
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    dispatcher.BeginInvoke(action);
                }
            };
        }
    }
}