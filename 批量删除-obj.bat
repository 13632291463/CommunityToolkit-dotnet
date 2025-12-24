@echo Off  
del /s /a  *.pdb *.ilk 2>nul  
FOR /R . %%d IN (.) DO rd /s /q "%%d/Obj" 2>nul  
FOR /R . %%d IN (.) DO rd /s /q "%%d/bin" 2>nul  
FOR /R . %%d IN (.) DO rd /s /q "%%d/.vs" 2>nul  
pause