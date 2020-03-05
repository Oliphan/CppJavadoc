// Guids.cs
// MUST match guids.h
using System;

namespace CppJavadoc
{
    static class GuidList
    {
        public const string guidCppJavadocPkgString = "70dbb5d8-42d5-43f8-af7b-37668d5c9b46";
        public const string guidCppJavadocCmdSetString = "7dda296b-3ba8-404a-a15b-bc88d13884b9";

        public static readonly Guid guidCppJavadocCmdSet = new Guid(guidCppJavadocCmdSetString);
    };
}