<?php
    if (isset($_GET["order"])) {
        $order = $_GET["order"];        
    }
    else {
        $order = 0;    
    }
    if (isset($_GET["start"])) {
        $start = $_GET["start"];        
    }
    else {
        $start = 1;    
    }
    ob_start();

    if ($order < 2) {
        $margin = 58.5;
        $orientation = "landscape";
        $size = "A4";
    }
    else {
        $margin = 28.5;
        $orientation = "portrait";
        $size = "A5";
    }
?>
<style type="text/css" media="print">
@page {
    margin-top: 0;
    margin-bottom: 0;
    margin-left: <?=$margin?>px;
    margin-right: <?=$margin?>px;
    orientation: <?=$orientation?>; 
    /* size: <?=$size?>; disables orientation option, and it can be wrong. */
}
</style>
<style>
    :root {
        --text: black;
        --bg: white;
        --border: white;
        --divbg: white;
    }
    @media (prefers-color-scheme: dark) {
        :root {
            --text: #fff;
            --bg: #000;
            --border: gray;
            --divbg: #800000;
        }
    }

    body {
        background-color: var(--bg);
        font-family: Garamond;
        margin: 0px;
    }
    table {
        border-spacing: 0px;  
        margin-top: 0px;
    }
    td { /* 820 x 520 */
        border: 0px solid #808080;  
        width: 400px;    
        height: 820px;  
        vertical-align: top;
        position: relative;
        padding-left: 60px;
        padding-right: 60px;
        padding-top: 72px;
        padding-bottom: 108px;        
        font-size: 16px;
        text-align: justify;
        line-height: 22px;
    }
    div {
        width: 400px;
        height: 640px;
        overflow-y: visible;
        color: var(--text);
        box-shadow: inset 0 0 0 1000px var(--divbg);
    }    
    div.border {
        position: absolute;
        left: 0px;
        top: 0px;
        width: 100%;
        height: 100%;
        box-shadow: inset 0 0 0 1px var(--border);
    }
    div.num {
        position: absolute;
        text-align: center;
        left: 0px;
        bottom: 46px;
        width: 100%;
        height: 16px;
        overflow: visible;
        box-shadow: inset 0 0 0 1000px transparent;
    }
    .header {
        font-size: 20px;
        font-weight: bold;
    }
    div.border_sample {
        position: absolute;
        text-align: center;
        left: 0px;
        top: 0px;
        width: 100%;
        height: 100%;
        box-shadow: inset 0 0 0 1000px #e0e0e0;
    }
    div.content_sample {
        position: absolute;
        text-align: center;
        width: 400px;
        box-shadow: inset 0 0 0 1000px #c0c0c0;
    }
    div.num_sample {
        position: absolute;
        text-align: center;
        left: 0px;
        bottom: 46px;
        width: 100%;
        height: 16px;
        overflow: visible;
        box-shadow: inset 0 0 0 1000px #c0c0c0;
    }
    .cover {
        position: absolute;
        left: 26px;
        top: 41px;
        width: 90%;
        height: 90%;
    }

</style>
<script>
document.addEventListener('visibilitychange', function() {
    if (document.visibilityState === 'visible') {
        // Page is active; perform actions to refresh content or maintain state
    }
});
</script>
<table width="<?php print $order < 2 ? 1040 : 520 ?>" align="center">
<?php
    $content = str_replace("\r", "", str_replace("<br />", "", file_get_contents("readme0.md")));
    $content = str_replace("[spacer]", '<img src="References/spacer.svg" width="1" />', $content);
    $content = str_replace("[spacer_h]", '<img src="References/spacer.svg" height="1" />', $content);
    
    $pos1 = strpos($content, "#");
    $pos2 = strpos($content, "\n",  $pos1);
    $header = substr($content, $pos1 + 2, $pos2 - $pos1 - 2);
    $content = "<span class=\"header\">$header</span>".substr($content, $pos2);

    $content = preg_replace_callback("/width=\"(\d+)\"/", function($matches) { return "width=\"".($matches[1]*100/21)."%\""; }, $content);
    $content = preg_replace_callback("/height=\"(\d+)\"/", function($matches) { return "height=\"".($matches[1]*400/21)."px\""; }, $content);

    $pos = 0;
    $endPos = strpos($content, "<!---->", $pos);
    $pageTexts = array();

    while ($endPos != false) {
        $pageTexts[] = nl2br(trim(substr($content, $pos, $endPos - $pos)));
        $pos = $endPos + 8;
        $endPos = strpos($content, "<!---->", $pos);
    }    
    $pageTexts[] = nl2br(trim(substr($content, $pos)));
    
    $output = "";

    if ($order == 0) { // 1 2 3 4, for reading in browser        
        for($i = ($start % 2 == 1) ? $start - 1 : $start - 2; $i < count($pageTexts); $i++) {
            if ($i % 2 == 0) {                
                $output.= "<tr><td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td>\n";
            }            
            else {
                $output.= "<td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td></tr>\n";
            }
        }
        if ($i % 2 == 1) {
            $output.= "<td class=\"right\"><div class=\"border\"></div></td></tr>\n";
        }
    }
    else if ($order == 1) { // 4 1 2 3, for printing on A4 paper on both sides
        $pages = array();

        for($i = ($start % 4 == 1) ? $start - 1 : ($start - 1) - ($start - 1) % 4; $i < count($pageTexts); $i++) {
            if ($i % 2 == 1) {
                if ($i % 4 == 1) {
                    $pages[] = "<tr><td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td>\n";
                }
                else {
                    $pages[] = "<tr><td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td>\n";
                }
            }
            else {
                $pages[] = "<td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td></tr>";
            }
        }
        if ($i % 4 == 1) {
            $pages[] = "<tr><td><div class=\"border\"></div></td>\n";
            $pages[] = "<td><div class=\"border\"></div></td></tr>\n";
            $pages[] = "<tr><td><div class=\"border\"></div></td>\n";
        }
        else if ($i % 4 == 2) {
            $pages[] = "<td><div class=\"border\"></div></td></tr>\n";
            $pages[] = "<tr><td><div class=\"border\"></div></td>\n";
        }
        else if ($i % 4 == 3) {
            $pages[] = "<tr><td><div class=\"border\"></div></td>\n";
        }

        $pageNum = count($pages) / 4;

        for ($i = 1; $i <= $pageNum; $i++) {
            $output.= $pages[$i*4 - 1].$pages[$i*4 - 4].$pages[$i*4 - 3].$pages[$i*4 - 2];
        }
    } 
    else if ($order == 2) {
        ?>
        <tr><td><div class="border_sample">Page: 130 x 205 mm<br />Screen resolution: 520 x 820 px</div><div class="content_sample">Content: 100 x 160 mm<br />Screen resolution: 400 x 640 px<br />Font: 16 px Garamond</div><div class="num_sample">Number box height: 4 mm / 16 px</div></td></tr>
        <tr><td><img class="cover" src="References/cover.svg"/></td></tr>
        <tr><td><div class="border"></div><div>Do you know in how many different ways you can fill a square grid, visiting each point once?<br />
        What is the recipe of generating them all without ever getting stuck?<br />
        <br />
        In this book I present a solution to one of the riddles of mathematics. It is complex in a way that you need to think about all possibilities but simple enough to understand if you just know the basic mathematical operations.<br />
        <br />
        The majority of work involved writing a computer program. This book contains the important points to take into consideration when doing so.
        </div></td></tr>
        <?php
        for($i = 0; $i < count($pageTexts); $i++) {
            //if ($i < 5)
            $output.= "<tr><td><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"num\">".($i+1)."</div></td></tr>\n";
        }
    }   
    print $output;
?> 
</table>
<?php
$htmlStr = ob_get_contents();
file_put_contents("A5print.html", $htmlStr);
?>
