using UnityEngine;

public class Singleton<T> where T : Singleton<T>, new()
{
    static T instnace;
    public static T Instance
    {
        get
        {
            if (instnace == null)
            {
                instnace = new T();
                instnace.init();
            }

            return instnace;
        }
    }

    protected virtual void init()
    {

    }
}