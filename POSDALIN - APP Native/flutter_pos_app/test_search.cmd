@echo off
cd /d "%~dp0"
flutter test test\search_service_test.dart --reporter expanded
