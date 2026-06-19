namespace WebFlex.UI.Controllers;

public static class MVCPath {
    internal static class Auth {
        public const string Login = "~/Views/Auth/Login.cshtml";
    }

    internal static class Main {
        public const string Index = "~/Views/Main/Index.cshtml";
    }

    internal static class Device {
        public const string DVC1000 = "~/Views/Device/DVC1000.cshtml";
        public const string DVC1010 = "~/Views/Device/DVC1010.cshtml";
        public const string DVC2000 = "~/Views/Device/DVC2000.cshtml";
        public const string DVC9000 = "~/Views/Device/DVC9000.cshtml";
    }

    internal static class Opc {
        public const string OPC1000 = "~/Views/Opc/OPC1000.cshtml";
        public const string OPC1020 = "~/Views/Opc/OPC1020.cshtml";
    }
}