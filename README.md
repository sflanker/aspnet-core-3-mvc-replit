# Install .Net 3.1
This sample repl demonstrates a Hack that allows you to use the new .NET3 sdk (3.1.407) with full dotnet cli and nuget support.

Clicking the Run command checks if latest sdk is installed (if not it is installed) and attempts to run current project. To run dotnet from the command prompt use the supplied script as follows

To verify the sdk version installed
```
$ ./dotnet --list-sdks
3.1.407 [/home/runner/dotnet/sdk]
```

## Run Sample Project
To run the sample mvc project installed in this repl you can either click Run or from the console execute the following command
```
$ ./dotnet run -p mvc
```

## Run in Watch Mode
You can also run the project in watch mode so it re-compiles and re-executes as you edit the project source. Not sure how viable this is as each change in the editor triggers a re-compilation.
```
$ ./dotnet watch -p mvc run
```


## Fork
Feel free to fork and create your own project using any of the dotnet cli templates. Read the note below on running a web project in repl.it
```
$ ./dotnet new

Getting ready...
Templates          Short Name          Language          Tags                  
-------------      --------------      ------------      ----------------------
Console Ap...      console             [C#], F#, VB      Common/Console        
Class library      classlib            [C#], F#, VB      Common/Library        
Worker Ser...      worker              [C#], F#          Common/Worker/Web     
Unit Test ...      mstest              [C#], F#, VB      Test/MSTest           
NUnit 3 Te...      nunit               [C#], F#, VB      Test/NUnit            
NUnit 3 Te...      nunit-test          [C#], F#, VB      Test/NUnit            
xUnit Test...      xunit               [C#], F#, VB      Test/xUnit            
Razor Comp...      razorcomponent      [C#]              Web/ASP.NET           
Razor Page         page                [C#]              Web/ASP.NET           
MVC ViewIm...      viewimports         [C#]              Web/ASP.NET           
MVC ViewStart      viewstart           [C#]              Web/ASP.NET           
Blazor Ser...      blazorserver        [C#]              Web/Blazor            
Blazor Web...      blazorwasm          [C#]              Web/Blazor/WebAssembly
ASP.NET Co...      web                 [C#], F#          Web/Empty             
ASP.NET Co...      mvc                 [C#], F#          Web/MVC               
ASP.NET Co...      webapp              [C#]              Web/MVC/Razor Pages   
ASP.NET Co...      angular             [C#]              Web/MVC/SPA           
ASP.NET Co...      react               [C#]              Web/MVC/SPA           
ASP.NET Co...      reactredux          [C#]              Web/MVC/SPA           
Razor Clas...      razorclasslib       [C#]              Web/Razor/Library     
ASP.NET Co...      webapi              [C#], F#          Web/WebAPI            
ASP.NET Co...      grpc                [C#]              Web/gRPC              
dotnet git...      gitignore                             Config                
global.jso...      globaljson                            Config                
NuGet Config       nugetconfig                           Config                
Dotnet loc...      tool-manifest                         Config                
Web Config         webconfig                             Config                
Solution File      sln                                   Solution              
Protocol B...      proto                                 Web/gRPC              

Examples:
    ./dotnet new mvc --auth Individual
    ./dotnet new proto
    ./dotnet new --help
    ./dotnet new mstest --help
```

## HTTPS Web Projects & Repl.it
The .NET web tempates try to run the project on ```https``` by default. This doesn't play nicely with ```repl.it``` and thus you need to make two small changes to the template.

### Startup.cs
Disable https redirection in the ```Configure(..)``` method
```
 // app.UseHttpsRedirection();
```

### Properties/launchSettings.json
Edit the mvc options object to remove https url from the ```applicationUrl``` and update as follows:
```
  "applicationUrl": "http://0.0.0.0:5000",
      
```

Now run the project and it should start normally and be assessible via the public repl url 