// Guids.cs
// MUST match guids.h
using System;

namespace CppJavadoc
{
    static class GuidList
    {
        public const string guidCppJavadocPkgString = "758530A3-2B09-4C72-9038-891382B3B432";
        public const string guidCppJavadocCmdSetString = "886634CF-D43A-400F-B932-37C06CDDEEE1";

        public static readonly Guid guidCppJavadocCmdSet = new Guid(guidCppJavadocCmdSetString);
    };
}