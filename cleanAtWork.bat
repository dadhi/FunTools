set SolutionFolder="D:\Dev\!2013\FunTools\"
for /d /r %SolutionFolder% %%d in (bin,obj) do @if exist "%%d" rd /s/q "%%d"