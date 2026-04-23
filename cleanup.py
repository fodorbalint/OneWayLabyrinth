'''
1. Make sure every case image has its .txt file. For this, implement .svg parsing in program, calculate possibilities at each step using the current rule set.
----- DONE -----
2. Make sure every .txt file has its image too (if they are used in the project)
3. Add size specification to txt files up to 2023-07-29
4. Add 2023_/2024_ prefix to early examples; rename in documentation too.
5. Delete files not used in readme0.md, Path.cs, Path_old.cs and Path_old2.cd
6. (optional) Add subfolder for files that have something in common and are not cases.
7. In Path.cs, some references are written without 2024_, while the file is correctly named. Find those with regex and correct. Path_old.cs has 821, which is a missing reference. Some refernces were incorrectly renamed, like in Path_old.cs 0730 -> 2023_0730 -> this refers to 2024_0730. Also 0710 and more
8. Review context: 2024_0712_1 has wrong possibility. Also 9_22362
9: 2025_0719 is not solved.
10: Some 9 walkthroughs are the same but have different number, like 2034435 or 2059934. One or both are wrong.
11. Corner discovery error when loading a completed walkhtrough from svg, like 9_22326. Also the possibilities of future lines will be put next to the live end, 2025_0527_future.
'''

import os
import re
import sys
import time
from datetime import datetime

def find_replace(findStr, replaceStr, contentStr):
    # string contains .svg
    pos = 0
    while pos != -1:
        pattern = re.compile(rf'[^_\d]{re.escape(findStr)}[^_\d]')
        match1 = pattern.search(contentStr, pos)
        pattern = re.compile(rf'[^_\d]{re.escape(findStr.replace(".svg", ""))}[^_\d]')
        match2 = pattern.search(contentStr, pos)

        if match1 != None and match2 != None:
            if match1.start() < match2.start():
                pos = match1.start()
            else:
                pos = match2.start()
                replaceStr = replaceStr.replace(".svg", "")            
        elif match1 != None:
            pos = match1.start()
        elif match2 != None:
            pos = match2.start()
            replaceStr = replaceStr.replace(".svg", "")
        else:
            pos = -1

        if pos != -1:
            environment = contentStr[pos-20:pos+25].replace("\n", " ")
            print("        \"" + environment + "\"")
            contentStr = contentStr[0:pos + 1] + replaceStr + contentStr[pos + len(findStr) - 3:]
            pos += len(replaceStr)
            
    return contentStr

prefix = "References/"
renameMode = False
if len(sys.argv) > 1 and sys.argv[1] == "-r":
    renameMode = True

contentList = ["MainWindow.xaml.cs", "readme.md", "readme0.md", "Path.cs", "Path_control.cs", "Path_old.cs", "Path_old2.cs", "Rules.xaml.cs"]

contentList = ["MainWindow.xaml.cs"]
contentList = []

files = []
for filename in os.listdir(prefix):
    files.append(filename)

# find ambigious references and resolve them manually

for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()

    print(f"----- {contentName} {len(content)}-----\n")

    pos = 0
    counter = 0
    while pos != -1:
        # 0711 should not be found as 2023_0711, 20711, ff0711, 07112 or 0711ff. \w includes _
        pattern = re.compile(r'[^\d\w]([01]\d{3}(_\d+)?)[^\d\w]')
        match = pattern.search(content, pos)

        if match != None:
            pos = match.start()
        else:
            pos = -1
        
        if pos != -1:
            counter += 1
            findStr = match.group(1)
            environment = content[pos-20:pos+25].replace("\n", " ")
            print("\"" + environment + "\"", end="")
            if findStr + ".svg" in files and "2024_" + findStr + ".svg" in files:
                print (f"    {findStr}    Duplicate found", end="")
            elif not(findStr + ".svg" in files) and not("2024_" + findStr + ".svg" in files):
                print (f"    {findStr}        Missing reference", end="")
            print("")
            
            pos += len(findStr)

    print(f"\n{counter} matches found.\n")

# rename files to long version

renameFilesFrom = []
renameFilesTo = []

for filename in files:
    if ".svg" in filename:
        txtName = filename.replace(".svg",".txt")
        if txtName in files:
            pass
            # print(filename + " " + str(files.index(txtName)))
        else:
            pass
            print(filename + ": txt not found")
            continue
    elif ".txt" in filename:
        svgName = filename.replace(".txt",".svg")
        if svgName in files:
            pass
            # print(filename + " " + str(files.index(svgName)))
        else:
            pass
            # print(filename + ": svg not found")
        continue
    elif not(".svg" in filename):
        if os.path.isfile(prefix + filename):
            pass
            # print(filename + " is other file type")
        else:
            pass
            # print(filename + " is a directory")
        continue
    
    filename = filename.replace(".svg", "")

    # rename file to full version, either by adding year or size (in case it is a walkthrough number)
    if not(filename[:2] == "9_" or filename[:5] == "2023_" or filename[:5] == "2024_" or filename[:5] == "2025_" or filename[:5] == "2026_"):
        # 1012 => 2024_1012 and 1012_1 => 2024_1012_1
        if (len(filename) == 4 or filename[4:5] == "_") and filename[:2].isdigit() and int(filename[:2]) <= 12:
            ts = os.path.getmtime(prefix + filename + ".svg")
            year = str(datetime.fromtimestamp(ts).year)
            # this might be a 9 walkthrough number, not a date
            if int(filename[:4]) >= 1000 and int(filename[:4]) <= 1231 and int(year) > 2023:
                print(f"{filename}: Attention: might be a 9 walkthrough")
                break
            renameFilesFrom.append(prefix + filename + ".svg")
            renameFilesTo.append(prefix + year + "_" + filename + ".svg")
            renameFilesFrom.append(prefix + filename + ".txt")
            renameFilesTo.append(prefix + year + "_" + filename + ".txt")
            # print(f"{filename} => {year + "_" + filename}")

        # 1861 => 9_1861 and 12345 => 9_12345
        elif filename[:2].isdigit():
            renameFilesFrom.append(prefix + filename + ".svg")
            renameFilesTo.append(prefix + "9_" + filename + ".svg")
            renameFilesFrom.append(prefix + filename + ".txt")
            renameFilesTo.append(prefix + "9_" + filename + ".txt")
            # print(f"{filename} => {"9_" + filename}")
    else:
        pass
        # format ok
        # print(f"    {filename}")

# print(f"{len(renameFilesFrom)} files to be renamed.\n")

for i in range (0, len(renameFilesFrom)):
    if renameMode:
        os.rename(renameFilesFrom[i], renameFilesTo[i])
    if not (".txt" in renameFilesFrom[i]):
        if i < len(renameFilesFrom) - 2:
            print(renameFilesFrom[i].replace(prefix, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(prefix, "").replace(".svg", ""))
        else:
            print(renameFilesFrom[i].replace(prefix, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(prefix, "").replace(".svg", ""), end = "")

sys.exit()

for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()
    
    print(f"----- {contentName} -----\n")
        
    inCount = 0
    newSvgCount = 0
    oldSvgCount = 0
    otherCount = 0
    noCount = 0
    
    for filename in files:
        if ".txt" in filename:
            svgName = filename.replace(".txt",".svg")
            if svgName in files:
                pass
                # print(filename + " " + str(files.index(svgName)))
            else:
                print(filename + ": svg not found")
            continue
        elif not(".svg" in filename):
            if os.path.isfile(prefix + filename):
                print(filename + " is other file type")
            else:
                print(filename + " is a directory")
            continue
        
        filename = filename.replace(".svg", "")

        # rename file to full version, either by adding year or size (in case it is a walkthrough number)
        if not(filename[:2] != "9_" or filename[:4] != "2023_" or filename[:4] != "2024_" or filename[:4] != "2025_" or filename[:4] != "2026_"):
            # 1012 => 2024_1012 and 1012_1 => 2024_1012_1
            if (len(filename) == 4 or filename[4:1] == "_") and int(filename[:2]) <= 12:
                ts = os.path.getmtime(prefix + filename)
                year = str(datetime.fromtimestamp(ts).year)
                # this might be a 9 walkthrough number, not a date
                if int(filename[:4]) >= 1000 and int(filename[:4]) <= 1231 and int(year) > 2023:
                    print(f"{filename}: Attention: might be a 9 walkthrough")
                    break
                renameFilesFrom.append(prefix + filename)
                renameFilesTo.append(prefix + year + "_" + filename)
                txtName = filename.replace(".svg",".txt")
                renameFilesFrom.append(prefix + txtName)
                renameFilesTo.append(prefix + year + "_" + txtName)

            # 1861 => 9_1861 and 12345 => 9_12345
            else:
                renameFilesFrom.append(prefix + filename)
                renameFilesTo.append(prefix + "9__" + filename)
                txtName = filename.replace(".svg",".txt")
                renameFilesFrom.append(prefix + txtName)
                renameFilesTo.append(prefix + "9_" + txtName)
                
        # in readme, we can have formats like:
        # 0724.svg, 2024_0724.svg, 0724, 2024_0724
        # 1861.svg, 9_1861.svg, 1861, 9_1861
        # in .cs files, we can have
        # 0724, 2024_0724
        # 1861, 9_1861
        # Due to the previous step that resolves ambiguity, now we can have 2023_0724 in the content, but the file is still called 0724
        # if the filename is 0724, the first search pattern will not find 2023_0724 or 0724_1
        # the second search pattern will find 2023_0724
        # we need to manually review the output and make sure that the filename is the full reference, not just the ending of it.

        match1 = None
        pattern = re.compile(rf'[^\d\w]{re.escape(filename)}[^\d\w]')
        match1 = pattern.search(content)
        pattern = re.compile(rf'_{re.escape(filename.replace(".svg", ""))}[^\d\w]')
        match2 = pattern.search(content)

        # short version is found
        if match1 != None and match2 == None:
            inCount += 1
            print(f"{filename}: short found")

            r'''
            inCount += 1
            if ".svg" in filename:
                # 2024_0724.svg, 2024_0724_1.svg, 2024_0724_1_1.svg, 2024_0711_rule 1.svg
                pattern = re.compile(r'^(202\d{1}|9)_\d{4}.*\.svg$')
                match = pattern.match(filename)
                if match:
                    newSvgCount += 1
                    print(filename, end = "")

                    txtName = filename.replace(".svg",".txt")
                    if txtName in files:
                        print("")
                        # print(": " + str(files.index(txtName)))
                    else:
                        print(": txt not found")
                else:
                    # matches 1005.svg, 27200_1.svg, but not 2n=2.svg, area big.svg
                    pattern = re.compile(r'^\d\d+.*\.svg$')
                    match1 = pattern.match(filename)
                    if match1:
                        print("    " + filename, end = "")
                        oldSvgCount += 1

                        # rename 0711 to 2024_0711 and 234876 to 9_234876
                        # exception: 1861.svg would be recognized as a date
                        # all walkthroughs of 9 x 9 were created from 2024. If files beetween 1000 and 1231 have modification year up to 2023, they are safe to rename.
                        pattern = re.compile(r'^[01]{1}\d{3}[_\.].*svg$')
                        match = pattern.match(filename)

                        if match and int(filename[:2]) <= 12:
                            ts = os.path.getmtime(prefix + filename)
                            year = str(datetime.fromtimestamp(ts).year)
                            if int(filename[:4]) >= 1000 and int(filename[:4]) <= 1231 and int(year) > 2023:
                                print(" error")
                                break
                            print(" => "+ year + "_" + filename)
                            content = find_replace(filename, year + "_" + filename, content)
                            renameFilesFrom.append(prefix + filename)
                            renameFilesTo.append(prefix + year + "_" + filename)
                            txtName = filename.replace(".svg",".txt")
                            if txtName in files:
                                renameFilesFrom.append(prefix + txtName)
                                renameFilesTo.append(prefix + year + "_" + txtName)
                        else:
                            print("     => 9_" + filename)
                            content = find_replace(filename, "9_" + filename, content)
                            renameFilesFrom.append(prefix + filename)
                            renameFilesTo.append(prefix + "9_" + filename)
                            txtName = filename.replace(".svg",".txt")
                            if txtName in files:
                                renameFilesFrom.append(prefix + txtName)
                                renameFilesTo.append(prefix + "9_" + txtName)

                        txtName = filename.replace(".svg",".txt")
                        if txtName in files:
                            pass
                            # print(": " + str(files.index(txtName)))
                        else:
                            print("txt not found")
                        
                    else:
                        print("        " + filename)
                        otherCount += 1
            '''

        elif match2 != None: 
            inCount += 1
            print(f"{filename}: long found")  
            
        else:
            noCount += 1 
            # print("----" + filename)

    print(f"New length {len(content)} inCount {inCount} newSvgCount {newSvgCount} oldSvgCount {oldSvgCount} otherCount {otherCount} noCount {noCount}\n")

    with open(contentName.replace(".cs", "_1.cs").replace(".md", "_1.md"), "w") as file:
        file.write(content)

print(f"--------- Renaming {len(renameFilesFrom)} files ----------\n")
for i in range (0, len(renameFilesFrom)):
    if renameMode:
        os.rename(renameFilesFrom[i], renameFilesTo[i])
    print(renameFilesFrom[i].replace(prefix, "") + " => " + renameFilesTo[i].replace(prefix, ""))