from PIL import Image, ImageDraw, ImageFont
import os

size=512
img=Image.new("RGBA",(size,size),(0,0,0,0))
draw=ImageDraw.Draw(img)

# Use macOS system font (Helvetica.ttc)
try:
    font = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 400)
except:
    # Fallback to default font if system font not available
    font = ImageFont.load_default()

bbox=draw.textbbox((0,0),"0",font=font)
x=(size-(bbox[2]-bbox[0]))//2
y=(size-(bbox[3]-bbox[1]))//2
draw.text((x,y),"0",font=font,fill=(255,255,255,255))
img.save("0.png")