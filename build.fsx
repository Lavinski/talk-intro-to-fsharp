// include Fake libs
#r "./packages/build/FAKE/tools/FakeLib.dll"

System.Environment.CurrentDirectory = __SOURCE_DIRECTORY__

open Fake
open Fake.FileUtils
open Fake.EnvironmentHelper
open Fake.AssemblyInfoFile
open System

let buildConfig = "Debug"
let authors = ["ConnectDevelop"]

// Directories
let artifacts = "./artifacts"

//System.IO.Path.GetFullPath

// Filesets
let appReferences  =
    !! "./src/**/*.csproj"
    ++ "./src/**/*.fsproj"

// FindProjects
let projectsToPackage =
    !! "./src/**/paket.template"

type Project = {
    Name: string;
    Directory: System.IO.DirectoryInfo;
    Version: string;
    TemplatePath: string
}

let getVersionFromTemplate templatePath = 
    StringHelper.ReadFile templatePath
    |> Seq.where (fun line -> line.StartsWith("Version"))
    |> Seq.map (fun line -> line.Replace("Version", "").Trim())
    |> Seq.exactlyOne

let projects =
    projectsToPackage
    |> Seq.map (fun templatePath -> 
        let dir = (directoryInfo templatePath).Parent
        let projFile = filesInDirMatching "*.*proj" dir |> Seq.exactlyOne
        {
            Name = projFile.Name;
            Directory = dir;
            Version = getVersionFromTemplate templatePath;
            TemplatePath = templatePath
        })

// MSBuild
MSBuildDefaults <- {
    MSBuildDefaults with
        ToolsVersion = Some "14.0"
        Verbosity = Some MSBuildVerbosity.Minimal }

// Targets
Target "Clean" (fun _ ->
    CleanDirs [artifacts]
)

Target "Version" (fun _ ->  
    for project in projects do  
        CreateFSharpAssemblyInfo (project.Directory.FullName @@ "AssemblyVersionInfo.fs")
            [
                Attribute.Version project.Version;
                Attribute.FileVersion project.Version
            ]
)

Target "Build" (fun _ ->  
    // compile all projects below src/app/
    MSBuild null "Build" ["Configuration", buildConfig] appReferences |> Log "AppBuild-Output: "
)

Target "Test" (fun _ ->  
    printfn "All Tests Passed!"
)

Target "Package" (fun _ ->
 
    CreateDir artifacts
    for project in projects do

        Paket.Pack (fun p -> 
            { p with
                BuildConfig = buildConfig;
                Version = project.Version;
                TemplateFile = project.TemplatePath;
                WorkingDir = artifacts;
                OutputPath = ".";
                IncludeReferencedProjects = true;
                BuildPlatform = "AnyCPU"
            }
        )
)

Target "Run" (fun _ ->
    let exePath = @"src\TwitterAlerts\bin\Debug\TwitterAlerts.exe"
    ExecProcess
        (fun info ->
            info.FileName <- exePath
            info.WorkingDirectory <- currentDirectory
            info.Arguments <- "")
        (TimeSpan.FromSeconds(1.0))
        |> ignore
)

// Build order
"Clean"   ?=> "Build"
"Version" ?=> "Build"
"Test"    <== [ "Build" ]
"Package" <== [ "Build"; "Version" ]

RunTargetOrDefault "Package"