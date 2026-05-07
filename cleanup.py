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

paths = ["References/", "References/countarea/", "References/checknearfield/", "References/rules/"]
renameMode = False
if len(sys.argv) > 1 and sys.argv[1] == "-r":
    renameMode = True

# make sure every svg file has its txt and vice-versa and rename files to long version
print("----- Checking file pairs and short versions -----\n")
index = 0
for path in paths:
    print(f"{path}\n") 
    files = []
    for filename in os.listdir(path):
        files.append(filename)

    renameFilesFrom = []
    renameFilesTo = []

    found = False
    for filename in files:
        if ".svg" in filename:
            if index <= 1:
                txtName = filename.replace(".svg",".txt")
                if txtName in files:
                    pass
                    # print(filename + " " + str(files.index(txtName)))
                else:
                    pass
                    found = True
                    print(filename + ": txt not found")
                    continue
        elif ".txt" in filename:
            if index <= 1:
                svgName = filename.replace(".txt",".svg")
                if svgName in files:
                    pass
                    # print(filename + " " + str(files.index(svgName)))
                else:
                    found = True
                    print(filename + ": svg not found")
            continue
        elif not(".svg" in filename):
            if os.path.isfile(path + filename):
                pass
                # print(filename + " is other file type")
            else:
                pass
                # print(filename + " is a directory")
            continue
        
        if index <= 2:
            filename = filename.replace(".svg", "")

            # rename file to full version, either by adding year or size (in case it is a walkthrough number)
            if not(filename[:2] == "7_"  or filename[:2] == "9_" or filename[:5] == "2023_" or filename[:5] == "2024_" or filename[:5] == "2025_" or filename[:5] == "2026_"):
                # 1012 => 2024_1012 and 1012_1 => 2024_1012_1
                if (len(filename) == 4 or filename[4:5] == "_") and int(filename[:2]) <= 12 and int(filename[2:4]) <= 31:
                    ts = os.path.getmtime(path + filename + ".svg")
                    year = str(datetime.fromtimestamp(ts).year)
                    # this might be a 9 walkthrough number, not a date
                    if int(filename[:4]) >= 1000 and int(filename[:4]) <= 1231 and int(year) > 2023:
                        print(f"{filename}: Attention: might be a 9 walkthrough")
                        break
                    renameFilesFrom.append(path + filename + ".svg")
                    renameFilesTo.append(path + year + "_" + filename + ".svg")
                    renameFilesFrom.append(path + filename + ".txt")
                    renameFilesTo.append(path + year + "_" + filename + ".txt")
                    # print(f"{filename} => {year + "_" + filename}")

                # 1861 => 9_1861 and 12345 => 9_12345
                elif filename[:2].isdigit():
                    renameFilesFrom.append(path + filename + ".svg")
                    renameFilesTo.append(path + "9_" + filename + ".svg")
                    renameFilesFrom.append(path + filename + ".txt")
                    renameFilesTo.append(path + "9_" + filename + ".txt")
                    # print(f"{filename} => {"9_" + filename}")
            else:
                pass
                # format ok
                # print(f"    {filename}")

    if found:
        print("")
    print(f"{len(renameFilesFrom)} files to be renamed.\n")

    for i in range (0, len(renameFilesFrom)):
        if renameMode:
            os.rename(renameFilesFrom[i], renameFilesTo[i])
        if not (".txt" in renameFilesFrom[i]):
            if i < len(renameFilesFrom) - 2:
                print(renameFilesFrom[i].replace(path, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(path, "").replace(".svg", ""))
            else:
                print(renameFilesFrom[i].replace(path, "").replace(".svg", "") + " => " + renameFilesTo[i].replace(path, "").replace(".svg", "") + "\n")

    index += 1

# find ambigious and missing references, and resolve them manually. 
print("----- List references in content -----\n")

contentList = ["MainWindow.xaml.cs", "Path.cs", "Path_control.cs", "Path_old.cs", "Path_old2.cs", "Rules.xaml.cs", "readme.md", "readme0.md"]

# contentList = ["MainWindow.xaml.cs", "Path.cs"]
# contentList = ["MainWindow.xaml.cs", "Path.cs", "Path_control.cs", "Path_old.cs", "Path_old2.cs", "Rules.xaml.cs"]
# contentList = ["readme.md", "readme0.md"]

prefixList = ["7_", "9_", "2023_", "2024_", "2025_", "2026_"]
# color codes or dimensions that are regex matched
# in readme, a lot are page numbers
exceptionList = ["360", "240", "100", "1000", "2000", "140", "440", "104", "121", "1445", "4300", "293", "695", "4000", "2688307514", "8288"]

# in .cs files, we can have
# 0711, 0711_1, 2024_0711, 2024_0711_1
# 743059, 743059_1, 9_743059, 9_743059_1
# in readme,".svg" can be added to it

# prevent matching at:
# "165": Page 165, page 165, 111 165, 111.165, 111/165, = 165, #0ff165, #0FF165, &nbsp;165, page 164-165
# in pattern 1:
# "0731": 120731
lookBehind = r'(?<!age )(?<!\d[ ./]|= )(?<![A-Za-z\d#;-])'
# used when matching an existing filename for its short form.
# if, 0618 was already replaced to 2023_0618, 2024_0618 will not be matched, but 0618 will, and it would be replaced to 2023_2024_0618 if _ wasn't excluded.
lookBehind2 = r'(?<!age )(?<!\d[ ./]|= )(?<![A-Za-z\d#;_-])'
# "165": 165 111, 165.111, 165/111, 165 = , #165ff0, #165FF0, 165&nbsp;,page 165-166
# in pattern1:
# "0731": 073112
# in pattern2:
# "2024": 2024_0624
lookAhead = r'(?![ ./]\d| =)(?![A-Za-z\d&_-])'
# Note: - needs to be at the end of the lookahead character class, otherwise it will create a range.

findStrList1 = []
findStrList2 = []

all_files = []
all_files_full = []
all_files_mask = []

for path in paths:
    for filename in os.listdir(path):
        all_files.append(filename)
        all_files_full.append(path + filename)
        all_files_mask.append(0)

contentI = 0
for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()

    print(f"{contentName}\n")

    pos = 0
    counter = 0
    shortCounter = 0

    findStrList1.append([])
    while pos != -1:
        # matches: 0711, 0711_1, 2024_0711, 2024_0711_1        
        pattern1 = re.compile(lookBehind + r'([01]\d{3}(_\d{,2})?)' + lookAhead)
        match1 = pattern1.search(content, pos)
        # above pattern will always match below pattern, but below pattern does not always match above pattern
        # 0711, 0711_1, 2024_0711, 2024_0711_1, 743059, 743059_1, 9_743059, 9_743059_1
        pattern2 = re.compile(lookBehind + r'(\d{3,}(_\d{,2})?)' + lookAhead)
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
            if pos2 < pos1:
                pos = pos2
                findStr = findStr2
            else:
                pos = pos1
                findStr = findStr1
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

            if content[pos - 1] != "_":
                shortCounter += 1
                findStrList1[contentI].append(findStr)
            counter += 1

            environment = content[pos-20:pos+25].replace("\n", " ")
            print("\"" + environment + "\"", end="")

            findCount = 0
            foundPrefix = ""
            for prefix in prefixList:
                if prefix + findStr + ".svg" in all_files:
                    foundPrefix += prefix[:-1] + ", "
                    findCount += 1

            if findCount >= 2:
                # if short version is found
                if content[pos - 1] != "_":
                    print (f"    {findStr}    Duplicate found: {foundPrefix[:-2]}", end="")
                else:
                    print (f"    {findStr}", end="")
            elif findCount == 0:
                print (f"    {findStr}    Missing reference", end="")
            else:
                print (f"    {findStr}", end="")
            print("")
            
            pos += len(findStr)

    
    if counter != 0:
        print("")
    print(f"{counter} matches found, {shortCounter} needs to be replaced.\n")
    contentI += 1

# rename references in contents to long version
print("----- Check if references are found in content -----\n")
contentI = 0
for contentName in contentList:
    with open(contentName, "r") as file:
        content = file.read()
    
    print(f"{contentName} {len(content)}\n")
        
    inCount = 0
    noCount = 0
    counter = 0
    shortCounter = 0

    findStrList2.append([])
    c = 0
    for filename in all_files:
        if not(".svg" in filename):
            c += 1
            continue

        search1 = filename.replace(".svg", "")
        # with replace only, 0427_1 would become 042
        if search1[0:2] == "7_":
            search2 = search1[2:]
        elif search1[0:2] == "9_":
            search2 = search1[2:]
        else:
            search2 = search1.replace("2023_", "").replace("2024_", "").replace("2025_", "").replace("2026_", "")
        search3 = all_files_full[c]
        searchPrefix = search3.replace(all_files[c], "")
                
        pos = 0
        found = False

        while pos != -1:
            match1 = None
            pattern = re.compile(lookBehind + rf'({re.escape(search1)})' + lookAhead)
            match1 = pattern.search(content, pos)
            match2 = None
            # 9_3.svg would match all occurences of 3, so in this case, we only look for the full version above.
            if len(search2) > 1:
                pattern = re.compile(lookBehind2 + rf'({re.escape(search2)})' + lookAhead)
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
                contextPrefix = content[pos-len(searchPrefix):pos].replace("\n", " ")
                if searchPrefix != contextPrefix:
                    # add subfolder
                    if contextPrefix[len(contextPrefix) - 11:] == "References/":
                        content = content[0:pos - 11] + searchPrefix + content[pos:]
                        pos += len(searchPrefix) - 11
                        print(f" => {searchPrefix}", end="")
                    else:
                        pass
                        # print(f" \"{searchPrefix}\" \"{contextPrefix}\"", end="")

                if findStr != search1:
                    findStrList2[contentI].append(findStr)
                    shortCounter += 1
                    print (f" => {search1}")
                    content = content[0:pos] + search1 + content[pos + len(findStr):]
                else:
                    print("")

                pos += len(search1)

        if found:
            inCount += 1
            all_files_mask[c] = 1
        else:
            noCount += 1
            # print(f"{search1} not found")

        c += 1

    if counter != 0:
        print("")
    print(f"{counter} matches found, {shortCounter} replaced.\n")
    print(f"New length {len(content)} inCount {inCount} noCount {noCount}\n")

    with open(contentName.replace(".cs", "_1.cs").replace(".md", "_1.md"), "w") as file:
        file.write(content)

    contentI += 1

for i in range (0, len(findStrList1)):
    findStrList1[i].sort()
    findStrList2[i].sort()
    if len(findStrList1[i]) == len(findStrList2[i]):
        found = False
        for j in range (0, max(len(findStrList1[i]), len(findStrList2[i]))):
            if findStrList1[i][j] != findStrList2[i][j]:
                print(f"{contentList[i]}: Items different: {findStrList1[i][j]} {findStrList2[i][j]}")
                found = True
                break
        
        if not found:
            print(f"{contentList[i]}: Arrays equal")

    else:
        print(f"{contentList[i]}: Arrays inequal length: {len(findStrList1[i])}, {len(findStrList2[i])}")
        for j in range (0, max(len(findStrList1[i]), len(findStrList2[i]))):
            if j < len(findStrList1[i]):
                print(f"{findStrList1[i][j]} ", end = "")
            else:
                print(f"      ", end = "")
            if j < len(findStrList2[i]):
                print(f"{findStrList2[i][j]} ", end = "")
            print("")

print("")
inCount = 0
noCount = 0
noCountFiles = []
for i in range (0, len(all_files_full)):
    if not(".svg" in all_files_full[i]):
        continue
    if all_files_mask[i] == 0:
        noCount += 1
        noCountFiles.append(all_files_full[i])
    else:
        inCount += 1

with open("filesNotFound.txt", "w", encoding="utf-8") as file:
    file.write("\n".join(noCountFiles))

print(f"Global inCount {inCount} noCount {noCount}")