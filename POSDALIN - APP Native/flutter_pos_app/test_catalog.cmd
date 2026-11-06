@echo off
cd /d "%~dp0"
flutter test test\catalog_service_test.dart --reporter expanded
