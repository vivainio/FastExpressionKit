import os

LOCAL_NUGET = "dev/localnuget"
if not os.path.isdir(LOCAL_NUGET):
    os.makedirs(LOCAL_NUGET)
os.chdir("FastExpressionKit.Test")
os.system("dotnet run")
