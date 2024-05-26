rmdir /S /Q "C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\mods\Signals"
xcopy /S /E /H "D:\dokumenty\ktane\Signals\build" "C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\mods" 

C:
cd "C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes"
ktane.exe
