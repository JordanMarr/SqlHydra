@echo off
pushd "%~dp0src\Build"
dotnet run -- Publish
popd
