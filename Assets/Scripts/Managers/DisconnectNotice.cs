namespace Managers
{
    public static class DisconnectNotice
    {
        public static bool IntentionalExitInProgress { get; set; }

        private static bool pending;

        public static void MarkUnexpected()
        {
            if (IntentionalExitInProgress)
                return;

            pending = true;
        }

        public static bool ConsumePending()
        {
            var wasPending = pending;
            pending = false;
            return wasPending;
        }
    }
}
