using System;

namespace L2Move.Helpers;

public class GeneralHelper
{
    public static string GetLocalDateNow()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }
    
    public static string GetDateNow()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }
}