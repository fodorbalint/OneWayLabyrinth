# The one-way labyrinth algorithm

This research aims to solve the following problem:<br />
"Draw a line that goes through an n x n grid (where n is an odd number), passing through each field once. The line has to start from the field at the upper left corner (1,1) and end at (n,n). At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields."

At first sight it may look easy. But look at the following example:

<img src="References/0701_1.svg"/>

<!---->

Based on the black line's movement, blue fragments were drawn to indicate a path we have to go through in the future in order to fill the board.<br />
Do you see why the situation is impossible from now on?

The question is, is there a single rule or a set of rules that will guarantee you can draw a labyrinth of any size? Or do the rules get infinitely complex?

To assist with the research, I have written a computer program. In the beginning, I let it run on a 21 x 21 field, and whenever I noticed a trouble, I coded the solution into it. While you can discover many patterns this way, they will be random and do not help in gaining a fundemental understanding. At one point you will find things get too complex, and you are still far from solving the 21 x 21 board.<br />
That's where a gradual approach comes in.

Due to the length of the study, it cannot be displayed on this one page. To continue reading, download the PDF files for <a href="A5 web.pdf"/>screen reading</a> or <a href="A5 print.pdf"/>to be printed out.</a>)

---

The project contains the source code for use with Visual Studio. To start the program, run OneWayLabyrinth.exe in the folder "bin/Debug/net6.0-windows".

Screenshots:

<img align="top" src="References/screenshot_main2.png" width="100%"/><br />
<img src="References/spacer.svg" height="54"/><br />
<img align="top" src="References/screenshot_rules2.png" width="100%"/>

---

Hotkeys:

Enter: Reload or Close error message<br />
Ctrl + S: Save path<br />
Right arrow: Step forward<br />
Left arrow: Step back<br />
Ctrl/Shift + arrows: step in direction if possible. If CapsLock is on, pressing the Ctrl or Shift keys is not necessary.<br />
Space: Run automatically / Stop automatic running
