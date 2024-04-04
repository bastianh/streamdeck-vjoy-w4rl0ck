namespace streamdeck_vjoy_w4rl0ck;

public sealed class Utility
{
    private Utility()
    {
    }

    #region Singleton

    public static Utility Instance
    {
        get
        {
            lock (LockObject)
            {
                return _instance ??= new Utility();
            }
        }
    }

    #endregion

    #region Singleton

    private static Utility _instance;
    private static readonly object LockObject = new();

    #endregion
}