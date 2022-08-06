namespace Wilgysef.Stalk.Core.Shared
{
    public static class StalkErrorCodes
    {
        #region Job Error Codes

        public const string JobAlreadyDone = "Stalk:000001";

        public const string JobNotPaused = "Stalk:000002";

        public const string JobActive = "Stalk:000003";

        #endregion

        #region Job Task Error Codes

        public const string JobTaskAlreadyDone = "Stalk:001001";

        public const string JobTaskNotPaused = "Stalk:001002";

        public const string JobTaskActive = "Stalk:001003";

        #endregion
    }
}
