<?php
// Prevent PHP from timing out
set_time_limit(0);

// Disable Apache/IIS output buffering configurations
if (function_exists('apache_setenv')) {
    apache_setenv('no-gzip', '1');
}
ini_set('output_buffering', 'off');
ini_set('zlib.output_compression', false);

// Set strict Server-Sent Events headers
header('Content-Type: text/event-stream');
header('Cache-Control: no-cache, no-transform'); // no-transform prevents proxy buffering
header('Connection: keep-alive');
header('X-Accel-Buffering: no'); // Disables buffering on Nginx/IIS if present

// Deep system flush command
function clear_system_buffers() {
    echo str_pad('', 4096); // Windows requires a minimum packet size to flush
    ob_flush();
    flush();
}

$signalFile = __DIR__ . '/new logs/tmp/new_data.txt';

if (file_exists($signalFile)) {
    $contents = file_get_contents($signalFile);
    unlink($signalFile);
    
    // Send the signal alert
    echo "data: " . json_encode([
        'status' => 'new_data_available',
        'filePath' => trim($contents)
    ]) . "\n\n";
    
    clear_system_buffers();
} else {
    // Send a fallback heartbeat ping so EventSource stays connected without hanging
    echo "data: " . json_encode(['status' => 'idle']) . "\n\n";
    clear_system_buffers();
}
?>
