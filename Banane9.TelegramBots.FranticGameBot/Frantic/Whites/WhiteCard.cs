namespace Banane9.TelegramBots.FranticGameBot.Frantic.Whites
{
    public abstract class WhiteCard
    {
        public abstract string StickerId { get; }

        public abstract void ExecuteEvent(FranticGame game);
    }
}