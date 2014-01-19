<?php
/********************************************************************************
 Copyright (C) 2012 Eric Bataille <e.c.p.bataille@gmail.com>

 This program is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program; if not, write to the Free Software
 Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.
********************************************************************************/

/*
 * Edit settings below this point.
 */
 
// This is the pattern for saving the files. Include the $ character if you want
// to use a counter, if it is not used, the file will be overwritten every time.
$filePattern = 'img$.png';

// This is the folder to place the file in, make sure that this points to a directory that the script has write access.
$uploadDir = '../../webservices/files/upload/';

// This is the path that is returned, appended with the just created filename. Don't include the filename here.
$basePath = 'http://thonoht.com/files/upload/';

/*
 * Don't edit anything below this point.
 */
 
// Get the raw data.
$data = file_get_contents('php://input');
if (strpos($filePattern, '$') == false) {
	// Fixed filename, simply upload.
	file_put_contents($uploadDir . $filePattern, $data);
	echo $basePath . $filePattern;
} else {
	// The pattern contains a $, so replace it with a counter.
	$handle = opendir($uploadDir);
	
	// Find the current maximum.
	$maxNumber = 0;
	while (($file = readdir($handle)) != false) {
		$pattern = '/' . str_replace('$', '(\\d+)', $filePattern) . '/';
		if (preg_match($pattern, $file, $matches) != false) {
			// This is an image file that matches the pattern, update the max number.
			$maxNumber = max($maxNumber, $matches[1]);
		}
	}
	$maxNumber++;
	
	// Save the file with the next number.
	$name = str_replace('$', $maxNumber, $filePattern);
	file_put_contents($uploadDir . $name, $data);
	echo $basePath . $name;
}
?> 