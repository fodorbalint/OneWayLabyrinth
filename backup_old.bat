:: Previous setup where the project in the user folder needed to be copied to OneDrive.
:: Console program files
xcopy "c:\Users\fodor\OneWayLabyrinth console\bin\Release\net7.0" "Console app" /h /i /c /k /r /y

:: Visual Studio files
xcopy "." "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth" /h /i /c /k /r /y
:: Manual deletion of unused files will be needed
xcopy "References" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth\References" /h /i /c /k /e /r /y
del "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth\Console app\*" /s /q
xcopy "Console app" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth\Console app" /h /i /c /k /e /r /y

:: Standalone program files
:: Should not be deleted if we are running the program there
del "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program\*" /s /q
xcopy "bin\Debug\net6.0-windows" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program" /h /i /c /k /r /y
:: Too much to store: xcopy "bin\Debug\net6.0-windows\completed" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program\completed" /h /i /c /k /r /y
xcopy "bin\Debug\net6.0-windows\incomplete" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program\incomplete" /h /i /c /k /r /y
xcopy "bin\Debug\net6.0-windows\rules" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program\rules" /h /i /c /k /e /r /y
xcopy "bin\Debug\net6.0-windows\runtimes" "c:\Users\fodor\OneDrive\Documents\OneWayLabyrinth project\program\runtimes" /h /i /c /k /e /r /y



pause