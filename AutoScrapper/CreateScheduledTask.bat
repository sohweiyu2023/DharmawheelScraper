@echo off
schtasks /Create /SC WEEKLY /D THU /TN "ScrapeAppTask" /TR C:\Users\cyber\source\repos\AutoScrapper\RunAutoScrapper.bat /RL HIGHEST /ST 09:00 /F
powershell -Command "Set-ScheduledTask -TaskName 'ScrapeAppTask' -Settings (New-ScheduledTaskSettingsSet -StartWhenAvailable)"
