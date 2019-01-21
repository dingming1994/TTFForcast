using System;
[assembly: log4net.Config.XmlConfigurator()]
namespace TTF.Utils
{
public class Logging
{
    public static log4net.ILog log = log4net.LogManager.GetLogger("TTF");
}
}