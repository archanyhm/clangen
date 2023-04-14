using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using WixSharp;
using File = WixSharp.File;

class Script
{
    static public void Main()
    {
        Build();
        //BuildWithAttributes();
    }

    static public void Build()
    {
        //note %ProgramFiles% will be mapped into %ProgramFiles64Folder% as the result of project.Platform = Platform.x64;
        var path = @"C:\Users\pmick\Documents\GitHub\clangen\dist\Clangen";

        var project =
            new ManagedProject("ClanGen",
                new Dir(@"%LocalAppData%\Programs\ClanGen\ClanGen",
                    new Files($@"{path}\*.*")
                ),
                new Dir(@"%Desktop%",
                    new ExeFileShortcut("Clangen", Path.Combine("[INSTALLDIR]", $"Clangen.exe"), arguments: "") { WorkingDirectory = "[INSTALLDIR]" }
                )
            );

        var versionNumber = "";
        
        if (Environment.GetEnvironmentVariable("version_number") != null)
        {
            versionNumber = Environment.GetEnvironmentVariable("version_number");
            project.Version = Version.Parse(versionNumber);
        }
        
        if (Environment.GetEnvironmentVariable("build_as_64") != null)
        {
            project.Platform = Platform.x64;
            project.OutFileName = $"ClanGen_{versionNumber}_x64";
        }
        else
        {
            project.Platform = Platform.x86;
            project.OutFileName = $"ClanGen_{versionNumber}_x86";
        }

        project.OutDir = "installers";

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        project.LicenceFile = $@"LICENSE.rtf";

        project.MajorUpgrade = new MajorUpgrade
        {
            Schedule = UpgradeSchedule.afterInstallInitialize,
            DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
        };
        
        project.WixSourceGenerated += InjectImages;

        project.InstallScope = InstallScope.perUser;

        project.BuildMsi();
    }
    
    private static void InjectImages(System.Xml.Linq.XDocument document)
    {
        var productElement = document.Root.Select("Product");

        // productElement.Add(new XElement("WixVariable",
            // new XAttribute("Id", "WixUIBannerBmp"),
            // new XAttribute("Value", @"Images\bannrbmp.bmp")));
        productElement.AddElement("WixVariable", @"Id=WixUIDialogBmp;Value=Images\dlgbmp.bmp");
    }

    static public void BuildWithAttributes()
    {
        //this sample is inly useful for the demonstration of how to work with AttributesDefinition and XML injection

        var project =
            new Project("MyProduct",
                new Dir(@"%ProgramFiles64Folder%\My Company\My Product",
                    new File(@"Files\Bin\MyApp.exe") { AttributesDefinition = "Component:Win64=yes" },
                    new Dir(@"Docs\Manual",
                        new File(@"Files\Docs\Manual.txt") { AttributesDefinition = "Component:Win64=yes" })));

        project.Package.AttributesDefinition = "Platform=x64";
        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889c");

        //Alternatively you can set Component Attribute for all files together (do not forget to remove "Component:Win64=yes" from file's AttributesDefinition)

        //either before XML generation
        //foreach (var file in project.AllFiles)
        //    file.Attributes.Add("Component:Win64", "yes");

        //or do it as a post-generation step
        //project.Compiler.WixSourceGenerated += new XDocumentGeneratedDlgt(Compiler_WixSourceGenerated);

        Compiler.BuildMsi(project);
    }

    static void Compiler_WixSourceGenerated(XDocument document)
    {
        document.Descendants("Component")
                .ForEach(comp => comp.SetAttributeValue("Win64", "yes"));
    }
}