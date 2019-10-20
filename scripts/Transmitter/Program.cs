using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public void Main(string argument)
        {
            var config = new Config(Me.CustomData);
            var accessCode = config.Get("AccessCode");
            var channel = config.Get("BroadcastChannel");
            var message = new BlastDoorRadioMessage() {
                AccessCode = accessCode,
                Command = argument,
            };

            IGC.SendBroadcastMessage(channel, message.Serialize(), TransmissionDistance.TransmissionDistanceMax);
        }
    }
}
