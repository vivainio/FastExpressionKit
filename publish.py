from __future__ import print_function

from pathlib import Path

import os, shutil

LOCAL_FEED = Path(r".\dev\localnuget").absolute()

if not LOCAL_FEED.exists():
    os.makedirs(LOCAL_FEED)

projects = ["FastExpressionKit", "FastExpressionKit.BulkInsert"]
version = "1.5.0"


def c(s):
    print(">", s)
    err = os.system(s)
    assert not err


def nuke(pth):
    if os.path.isdir(pth):
        shutil.rmtree(pth)


def nuget_add(pth):
    c(f"dotnet nuget push -s {LOCAL_FEED} {pth}")


startdir = Path(".").absolute()

for prjdir in projects:
    os.chdir(startdir / prjdir)
    nuke("bin")
    nuke("obj")

    def pack():
        c(f"dotnet pack -c Release /p:Version={version} /p:PubVer={version}")
        pkgs = list(Path("bin/Release").glob("*.nupkg"))
        nuget_add(pkgs[0])

    pack()
