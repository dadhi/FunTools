set SolutionFolder="D:\Dev\!2014\FunTools\"
for /d /r %SolutionFolder% %%d in (bin,obj) do @if exist "%%d" rd /s/q "%%d"