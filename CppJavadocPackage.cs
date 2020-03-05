namespace CppJavadoc
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Runtime.InteropServices;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(GuidList.guidCppJavadocPkgString)]
    public sealed class CppJavadocPackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();
        }
    }
}