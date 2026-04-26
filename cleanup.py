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
12: 2024_0612.txt has no svg file
'''

import os
import re
import sys
import time
from datetime import datetime

prefix = "References/"
renameMode = False
if len(sys.argv) > 1 and sys.argv[1] == "-r":
    renameMode = True

files = []
for filename in os.listdir(prefix):
    files.append(filename)

# rename files to long version
r'''
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

print(f"{len(renameFilesFrom)} files to be renamed.\n")

for i in range (0, len(renameFilesFrom)):
    if renameMode:
        os.rename(renameFilesFrom[i], renameFilesTo[i])
    if not (".txt" in renameFilesFrom[i]):
        if i < len(renameFilesFrom) - 2:
            print(renameFilesFrom[i].replace(prefix, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(prefix, "").replace(".svg", ""))
        else:
            print(renameFilesFrom[i].replace(prefix, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(prefix, "").replace(".svg", ""), end = "")
'''

contentList = ["MainWindow.xaml.cs", "readme.md", "readme0.md", "Path.cs", "Path_control.cs", "Path_old.cs", "Path_old2.cs", "Rules.xaml.cs"]

contentList = ["MainWindow.xaml.cs"]
# contentList = []

prefixList = ["7_", "9_", "2023_", "2024_", "2025_", "2026_"]
# color codes or dimensions that are regex matched
exceptionList = ["100", "100000", "1048", "360", "688", "240", "255", "000000", "1000", "996600", "008000", "2000", "808000", "140"]
findStrList1 = []
findStrList2 = []

# find ambigious and missing references, and resolve them manually. 

for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()

    print(f"----- {contentName} -----\n")

    pos = 0
    counter = 0
    shortCounter = 0

    while pos != -1:
        # 0711 should not be found as 20711, ff0711, 07112 or 0711ff, but it should be found as 2023_0711_1
        pattern1 = re.compile(r'[^a-zA-Z0-9]([01]\d{3}(_\d+)?)[^\w]')
        match1 = pattern1.search(content, pos)
        pattern2 = re.compile(r'[^a-zA-Z0-9](\d{3,})[^\w]')
        match2 = pattern2.search(content, pos)
        
        findStr = ""
        if match1 != None or match2 != None: 
            pos1 = pos2 = len(content)
            if match1 != None:
                pos1 = match1.start()
                findStr1 = match1.group(1)
            if match2 != None:
                pos2 = match2.start()
                findStr2 = match2.group(1)
            if pos1 < pos2:
                pos = pos1
                findStr = findStr1
            else:
                pos = pos2
                findStr = findStr2
        else:
            pos = -1
        
        if pos != -1:
            found = False
            for item in exceptionList:
                if item == findStr:
                    found = True

            if found:
                pos += len(findStr)
                continue

            if content[pos] != "_":
                shortCounter += 1
                findStrList1.append(findStr)
            counter += 1

            environment = content[pos-20:pos+25].replace("\n", " ")
            print("\"" + environment + "\"", end="")

            findCount = 0
            for prefix in prefixList:
                if prefix + findStr + ".svg" in files:
                    findCount += 1

            if findCount >= 2:
                # if short version is found
                if content[pos] != "_":
                    print (f"    {findStr}    Duplicate found: {findCount}", end="")
                else:
                    print (f"    {findStr}", end="")
            elif findCount == 0:
                print (f"    {findStr}    Missing reference", end="")
            else:
                print (f"    {findStr}", end="")
            print("")
            
            pos += len(findStr)

    print(f"\n{counter} matches found, {shortCounter} needs to be replaced.\n")

# rename references in contents to long version
for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()
    
    print(f"----- {contentName} {len(content)} -----\n")
        
    inCount = 0
    noCount = 0
    counter = 0
    shortCounter = 0

    for filename in files:
        if not(".svg" in filename):
            continue
        
        search1 = filename.replace(".svg", "")
        # with replace only, 0427_1 would become 042
        if search1[0:2] == "7_":
            search2 = search1[2:]
        elif search1[0:2] == "9_":
            search2 = search1[2:]
        else:
            search2 = search1.replace("2023_", "").replace("2024_", "").replace("2025_", "").replace("2026_", "")
                
        # in readme, we can have formats like:
        # 0724.svg, 2024_0724.svg, 0724, 2024_0724
        # 1861.svg, 9_1861.svg, 1861, 9_1861
        # in .cs files, we can have
        # 0724, 2024_0724
        # 1861, 9_1861
        
        # match1 finds the full name. match2 finds only the short version, and not the full name, because \w excludes _
        # we need to manually review the output and make sure that the filename is the full reference, not just the ending of it.

        pos = 0
        found = False

        while pos != -1:
            match1 = None
            pattern = re.compile(rf'[^\w]({re.escape(search1)})[^\w]')
            match1 = pattern.search(content, pos)
            match2 = None
            # 9_3.svg would match all occurences of 3.
            if len(search2) > 1:
                pattern = re.compile(rf'[^\w]({re.escape(search2)})[^\w]')
                match2 = pattern.search(content, pos)

            findStr = ""
            if match1 != None or match2 != None: 
            # the file can be referenced multiple times in the file, both in the long and short version. We take the first occurrence
                pos1 = pos2 = len(content)
                findStr1 = findStr2 = ""
                if match1 != None:
                    pos1 = match1.start()
                    findStr1 = match1.group(1)
                if match2 != None:
                    pos2 = match2.start()
                    findStr2 = match2.group(1)
                if pos1 < pos2:
                    pos = pos1
                    findStr = findStr1
                else:
                    pos = pos2
                    findStr = findStr2
            else:
                pos = -1 
            
            if pos != -1:
                found = True
                counter += 1
                environment = content[pos-20:pos+25].replace("\n", " ")
                print("\"" + environment + "\"", end="")
                if findStr != search1:
                    findStrList2.append(findStr)
                    shortCounter += 1
                    print (f" => {search1}")
                    content = content[0:pos + 1] + search1 + content[pos + 1 + len(findStr):]
                else:
                    print("")

                pos += len(search1)

        if found:
            inCount += 1
        else:
            noCount += 1
            # print(f"{search1} not found")

    print(f"\n{counter} matches found, {shortCounter} replaced.\n")
    print(f"New length {len(content)} inCount {inCount} noCount {noCount}")

    with open(contentName.replace(".cs", "_1.cs").replace(".md", "_1.md"), "w") as file:
        file.write(content)

    findStrList1.sort()
    findStrList2.sort()
    if len(findStrList1) == len(findStrList2):
        print("Arrays equal length")

        for i in range (0, max(len(findStrList1), len(findStrList2))):
            if findStrList1[i] != findStrList2[i]:
                print(f"Items different: {findStrList1[i]} {findStrList2[i]}")
                break
    else:
        print("Arrays inequal length")
        for i in range (0, max(len(findStrList1), len(findStrList2))):
            if i < len(findStrList1):
                print(f"{findStrList1[i]} ", end = "")
            if i < len(findStrList2):
                print(f"{findStrList2[i]} ", end = "")
            print("")