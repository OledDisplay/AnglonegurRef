Frenskibot isa program that writes conspectuses based on the Prosveta history book.
Materials to generate conspectuses without the use of this program are included in this dev build.

Usage guide:
In orther to combat the device limit on the Prosveta site, the program is build to extract the whole textbook in one sitting (to be contained localy), so find time to do that upon initial usage.
Contact the FrenskiBot owner when you wish to download the textbook.

Open and run the ConsoleApp1.csproj file to start the generation process
You will be asked to either download the textbook dinamicly or use a local download. With a local download you'd download the whole textbook, so if you have limited prosveta access, use this. A dynamic download would requiere guaranteed access.
Upon choosing a local download you'd have to wait a bit for the textbook to download. LET THIS PROCESS RUN, DO STOP IT, AS THIS WOULD MEAN STARTING FROM THE BEGGING (as of the current build)

You will then be asked for the number of the lessons you want a conspectus on. At this point the program will start the process of opening and reading the pages of that lesson so stand by, while stuff is getting sorted.

At this process you should setup your generation folder. Open ApiMaterialsProg\. Change the contents of plan.txt to the plan from google classroom. 
You will also have to go the writing style folder and put at least one text file with an example conspectus or text you wrote that will be used to build the new conspectus (extraction of writing style)

Confirm with any input that you've added an example text(s). You will be asked to submit a size (in words) for the conspectus.
Fo

This is not the final build of the project, so the code doesn't contain notes, regarding ease of usage. On final release descriptive notes will allow easy addaptation to other textbooks.
