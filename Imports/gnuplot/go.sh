#!/bin/bash

rm -r texture_border; mkdir texture_border
rm -r texture_border_negate; mkdir texture_border_negate

for IMAGE in $(ls texture/*.png); do
    BASE=$(basename $IMAGE)
    echo $BASE

    convert "texture/$BASE" -bordercolor none -border 55x186 "texture_border/$BASE"
    convert "texture_border/$BASE" -channel a -negate +channel -fill black -colorize 100% "texture_border_negate/$BASE"
done

montage manual/*.png -tile 4x -geometry +2+2 ./montage_manual.png
montage texture_border/*.png -tile 8x -geometry +0+0 -background none ./texture_border_montage.png
montage texture_border_negate/*.png -tile 8x -geometry +0+0 -background none ./texture_border_negate_montage.png
