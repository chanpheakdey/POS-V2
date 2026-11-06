@echo off
cd /d "%~dp0"
dart run build_runner build --delete-conflicting-outputs
