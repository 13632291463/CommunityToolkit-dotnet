using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfApp.ViewModels
{
    public partial class MessengerViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string _receivedMessage = string.Empty;

        public MessengerViewModel() : base()
        {
            // 注册消息接收器
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<string>>(this, (r, m) =>
            {
                ReceivedMessage = $"收到消息: {m.Value}";
            });
        }
        //public MessengerViewModel(IMessenger messenger) : base(messenger)
        //{
        //    // 注册消息接收器
        //    this.Messenger.Register<ValueChangedMessage<string>>(this, (r, m) =>
        //    {
        //        ReceivedMessage = $"收到消息: {m.Value}";
        //    });
        //}
       

        [RelayCommand]
        private void SendMessage()
        {
            this.Messenger.Send(new ValueChangedMessage<string>(Message));
            Message = string.Empty;
        }
    }
}