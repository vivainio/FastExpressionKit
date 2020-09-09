import subprocess
import os

os.environ["DOTNET_NOLOGO"] = "true"
LOCAL_NUGET = "dev/localnuget"
if not os.path.isdir(LOCAL_NUGET):
    os.makedirs(LOCAL_NUGET)
os.chdir("FastExpressionKit.Test")
subprocess.check_call(["dotnet", "run"])