// FAKE Build Script
// Based off https://github.com/SAFE-Stack/SAFE-BookStore/blob/master/build.fsx with minor changes

#r @"packages/build/FAKE/tools/FakeLib.dll"
#r @"packages/build/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

open Fake
open Fake.ReleaseNotesHelper
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.IO

// -- Project Information
let project = "Personal Website - William Tetlow"

let summary = "Personal Website built with F#"

let description = "Exclusively F#, Client built using Fable, Elmish & React. Server built using .NET Core & Giraffe."
// ------------------------------------------------------------------------------------------------------------------
// -- Project Paths
let slnPath = "./" |> FullName

let clientRootPath = "./src/Client" |> FullName

let clientProjectPath = (+) clientRootPath "/src" |> FullName

let webAppProjectPath = "./src/WebApplication" |> FullName
// ------------------------------------------------------------------------------------------------------------------
// -- CLI Helpers
let run' timeout cmd args dir =
    if execProcess (fun info ->
        info.FileName <- cmd
        if not (String.IsNullOrWhiteSpace dir) then
            info.WorkingDirectory <- dir
        info.Arguments <- args
    ) timeout |> not then
        failwithf "Error while running '%s' with args: %s" cmd args

let run = run' System.TimeSpan.MaxValue

let validateCliArgs args =
  args |> Seq.iter (fun str -> if String.IsNullOrEmpty str then failwith "Ensure all CLI arguments are supplied for command")
// ------------------------------------------------------------------------------------------------------------------
// -- Dotnet CLI Setup
let dotnetcliVersion : string =
  try
    let content = File.ReadAllText "global.json"
    let json = Newtonsoft.Json.Linq.JObject.Parse content
    let sdk = json.Item("sdk") :?> JObject
    let version = sdk.Property("version").Value.ToString()
    version
  with
  | exn -> failwithf "Could not parse global.json: %s" exn.Message

let mutable dotnetExePath = "dotnet"

let runDotnet workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "dotnet %s failed" args
// ------------------------------------------------------------------------------------------------------------------
// -- Docker CLI Setup
let dockerUsername = getBuildParam "-docker-u"

let dockerPassword = getBuildParam "-docker-p"

let dockerRepo = getBuildParam "-docker-repo"

let dockerImageVersion = getBuildParam "-docker-img-v" // Format = 0.0

let dockerImageName = getBuildParam "-docker-img-n" // Used running image locally

let dockerExePath = "docker"

let runDocker workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- dockerExePath
            info.UseShellExecute <- false
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "docker %s failed" args
// ------------------------------------------------------------------------------------------------------------------
// -- Build Step Helpers
let platformTool tool winTool =
    let tool = if isUnix then tool else winTool
    tool
    |> ProcessHelper.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let yarnTool = platformTool "yarn" "yarn.cmd"

do if not isWindows then
    // We have to set the FrameworkPathOverride so that dotnet sdk invocations know
    // where to look for full-framework base class libraries
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName(mono) </> ".." </> "lib" </> "mono" </> "4.5"
    setEnvironVar "FrameworkPathOverride" frameworkPath

// Read additional information from the release notes document
let releaseNotes = File.ReadAllLines "RELEASE_NOTES.md"

let releaseNotesData =
    releaseNotes
    |> parseAllReleaseNotes

let release = List.head releaseNotesData

let packageVersion = SemVerHelper.parse release.NugetVersion
// ------------------------------------------------------------------------------------------------------------------
// -- Clean Project Steps
Target "Clean" (fun _ -> 
  runDotnet slnPath "clean"
  CleanDirs ["docs/output"; (webAppProjectPath + sprintf "/WebRoot/dist");]
)
// ------------------------------------------------------------------------------------------------------------------
// -- Build Project Steps
Target "InstallDotNetCore" (fun _ ->
    dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion
)

Target "BuildServer" (fun _ ->
    runDotnet webAppProjectPath "restore"
    runDotnet webAppProjectPath "build"
)

Target "InstallClient" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target "BuildClient" (fun _ ->
    runDotnet clientProjectPath "restore"
    runDotnet clientProjectPath "fable npm-run build-client:dev"
)
// ------------------------------------------------------------------------------------------------------------------
// -- Run Project Steps
FinalTarget "KillProcess" (fun _ ->
    killProcess "dotnet"
    killProcess "dotnet.exe"
)

Target "Run" (fun _ -> 
  let serverWatch = async { runDotnet webAppProjectPath "watch run" }
  let fableWatch = async { runDotnet clientProjectPath "fable npm-run start-client:dev" }

  Async.Parallel[| serverWatch; fableWatch; |]
  |> Async.RunSynchronously
  |> ignore)
// ------------------------------------------------------------------------------------------------------------------
// -- Publish Project Steps
Target "BundleClient" (fun _ ->
  runDotnet clientProjectPath "fable npm-run build-client:prod"
)

Target "PublishWebApplication" (fun _ ->
  runDotnet webAppProjectPath "publish -c Release"
)

// **- Docker Steps Require You To Create Repository First -**

Target "DockerLogin" (fun _ ->
  validateCliArgs [| dockerUsername; dockerPassword; |]
  runDocker slnPath <| sprintf "login --username %s --password %s" dockerUsername dockerPassword
)

Target "BuildDockerImage" (fun _ -> 
  validateCliArgs [| dockerUsername; dockerRepo; dockerImageVersion; |]
  runDocker slnPath <|  sprintf "build ./ -t %s/%s:v%s" dockerUsername  dockerRepo  dockerImageVersion
)

Target "RunDockerImage" (fun _ ->
  validateCliArgs [| dockerImageName; dockerUsername; dockerRepo; dockerImageVersion; |]
  runDocker slnPath 
  <|  sprintf "run -p=8080:8080 --name %s %s/%s:v%s --ip 0.0.0.0" dockerImageName dockerUsername dockerRepo dockerImageVersion

  // **- Add e2e tests here
)

Target "DeployDockerImage" (fun _ ->
  validateCliArgs [| dockerUsername; dockerRepo; dockerImageVersion; |]
  runDocker slnPath <|  sprintf "push %s/%s:v%s"  dockerUsername  dockerRepo  dockerImageVersion
)

// ------------------------------------------------------------------------------------------------------------------
// -- FAKE Steps
Target "InitInstall" DoNothing
Target "Build" DoNothing
Target "Publish" DoNothing
Target "BuildImageAndDeployToDocker" DoNothing

"InstallDotNetCore"
  ==> "InstallClient"
  ==> "InitInstall"

"Clean"
  ==> "BuildClient"
  ==> "BuildServer"
  ==> "Build"
  ==> "Run"

"Clean"
  ==> "BundleClient"
  ==> "PublishWebApplication"
  ==> "Publish"

"DockerLogin"
  ==> "BuildDockerImage"  // -docker-u= -docker-p= -docker-rep= -docker-img-v=
  ==> "DeployDockerImage" // -docker-u= -docker-p= -docker-rep= -docker-img-v=
  ==> "BuildImageAndDeployToDocker"    // -docker-u= -docker-p= -docker-rep= -docker-img-v=

RunTargetOrListTargets()





