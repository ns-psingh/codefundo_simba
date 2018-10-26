import cv2 as cv
import utils
import detectvegetation
import segmentcolor
import detectpolygon
import numpy as np
import matplotlib.pyplot as plt
from collections import defaultdict
def show_image(img, label='image'):
    plt.imshow(img)
    plt.show()
    cv.waitKey(0)

# smoothing using filter
def smooth(img, filter_type):
    if filter_type == "mean":
        return cv.blur(img, (5,5))
    if filter_type == "gaussian":
        return cv.GaussianBlur(img, (5,5), 0)
    if filter_type == "median":
        return cv.medianBlur(img, 5)
    if filter_type == "bilateral":
        return cv.bilateralFilter(img, 9, 75, 75)
    return bilateral_filter

# return the mode pixel of the image
def get_mode(img, xdim, ydim):
    # split into color channels
    [B,G,R] = cv.split(img)
    blue = B.astype(float)
    green = G.astype(float)
    red = R.astype(float)
  
    # count the number of times each triple shows up
    d = defaultdict(int)
    for i in range(xdim):
        for j in range(ydim):
            d[(B[i,j], G[i,j], R[i,j])] += 1

    # return the triple which shows up most often
    maxval = 0
    returnval = (0,0,0)
    for k,v in d.items():
        if v > maxval:
            returnval = k
            maxval = v
    return returnval

def plot_histogram(img):
  color = ('b','g','r')
  for i,col in enumerate(color):
      histr = cv.calcHist([img],[i],None,[256],[0,256])
      plt.plot(histr,color = col)
      plt.xlim([0,256])
  plt.show()

  
  
def detectbui(segmented, original, xdim, ydim):

  # morphological opening and closing
  kernel = np.ones((3,3), np.uint8)
  img = cv.morphologyEx(segmented, cv.MORPH_OPEN, kernel)
  img = cv.morphologyEx(img, cv.MORPH_CLOSE, kernel)

  show_image(img, 'open-close')

  imgcopy = img.copy()
  gray = cv.cvtColor(img, cv.COLOR_RGB2GRAY)

  num_buildings = 0

  for i in range(255):
    # threshold the grayscale image at that value
    binary = np.zeros((xdim, ydim), np.uint8)
    ret, binary = cv.threshold(gray, dst=binary, thresh=i, maxval=255, type=cv.THRESH_OTSU)
    #binary[gray == i] = 255
    # utils.show_image(binary, 'binary')

    # find contours, fit to polygon, and determine if rectangular
    contours, hierarchy = cv.findContours(binary, mode=cv.RETR_LIST, method=cv.CHAIN_APPROX_SIMPLE)

    for c in contours:
      poly = cv.approxPolyDP(np.array(c), 0.07*cv.arcLength(c,True), True)
      carea = cv.contourArea(c)
      polyarea = cv.contourArea(poly)
      hull = cv.convexHull(c)
      hullarea = cv.contourArea(hull)

      # bounding box
      rect = cv.minAreaRect(c)
      box = cv.cv.BoxPoints(rect)
      box = np.int0(box)

      if polyarea > 30 and carea > 30:
        cv.drawContours(img, [c], 0, (0,0,255), 1)
      if len(poly) < 6 and carea > 100: #and carea > 5: #\
          #and abs(polyarea/carea - 1) < 0.25:
        num_buildings += 1
        cv.drawContours(imgcopy, [poly], 0, (0,0,255), 1)
        cv.drawContours(original, [poly], 0, (0,0,255), 1)

  # show images
  #show_image(img, 'all bounding boxes')
  #show_image(imgcopy, 'with some filtering')
  #show_image(original, 'onto original')
  print(num_buildings)
  return original
def vegetationMask(im, xdim, ydim):
  # compute color invariant
  [B,G,R] = cv.split(im)
  red = R.astype(float)
  blue = B.astype(float)
  green = G.astype(float)

  colInvarIm = np.zeros(shape=(xdim, ydim))

  # iterate over the image
  for i in range(xdim):
    for j in range(ydim):
      # if there are no blue or green at thix pixel, turn it black
      if (green[i,j] + blue[i,j]) < np.finfo(float).eps:
        colInvarIm[i,j] = 2
      else:
        if blue[i,j] > 130 and blue[i,j] < 150:
          im[i,j] = blue[i,j] #(4./np.pi)*np.arctan((blue[i,j] - green[i,j])/(green[i,j] + blue[i,j]))
        else:
          im[i,j] = 2

  #plt.imshow(im)
  #plt.show()
  # normalize to [0,255]
  colInvarIm += abs(colInvarIm.min())
  colInvarIm *= 255.0/colInvarIm.max()
  colInvarIm = colInvarIm.astype('uint8')

  # threshold to detect vegetation
  thresh, vegetation = cv.threshold(colInvarIm, 0, 255, cv.THRESH_OTSU)
  #plt.imshow(colInvarIm)
  #plt.show()
  #plt.imshow(vegetation)
  #plt.show()
  #cv.destroyAllWindows()

  cinvar_fname = fname[:-4] + '-col-invar.png'
  #cv.imwrite(cinvar_fname, colInvarIm)
  mask_fname = fname[:-4] + '-veg-mask.png'
  #cv.imwrite(mask_fname, vegetation)

  return vegetation

def mask(img, xdim, ydim):

  #plot_histogram(img)

  [B,G,R] = cv.split(img)
  blue = B.astype(float)
  green = G.astype(float)
  red = R.astype(float)

  meanR = np.mean(red)
  stdR = np.std(red)
  #print(meanR + 1.6 * stdR)
  meanB = np.mean(blue)
  stdB = np.std(blue)
  #print(meanB + 1.1 * stdB)

  mode_pixel =get_mode(img, xdim, ydim)

  # separate into roads and houses
  for i in range(xdim):
    for j in range(ydim):
      # road: red value is at least 2 std above the mean
      if red[i,j] > meanR + 1.6 * stdR: # red[i,j] > 180
        img[i,j] = mode_pixel
      # houses: blue value is at least 1 std above the mean
      if blue[i,j] > meanB + 1.1 * stdB: # 182: #and blue[i,j] <= 238:
        img[i,j] = (0,0,0)

  cv.imshow("mask",img)

  return img
def detectve(img, xdim, ydim):
    # convert to grayscale
    gray = cv.cvtColor(img,cv.COLOR_BGR2GRAY)
    #plt.imshow(gray)
    #plt.show()
    
    # threshold to convert to binary image
    ret, thresh = cv.threshold(gray,0,255,cv.THRESH_BINARY_INV+cv.THRESH_OTSU)
    #plt.imshow(thresh)
    #plt.show()

    # erode image to isolate the sure foreground
    kernel = np.ones((3,3),np.uint8)
    opening = cv.morphologyEx(thresh,cv.MORPH_OPEN, kernel, iterations=3)
    #plt.imshow(opening)
    #plt.show()

    # get the median pixel value (should be background)
    mode = get_mode(img, xdim, ydim)

    # replace the foreground (trees) with the median pixel
    for i in range(xdim):
        for j in range(ydim):
            # if it's white in the eroded image, then it's vegetation
            if opening[i,j] == 255:
                # set to black
                img[i,j] = mode

    #plt.imshow(img)
    #plt.show()
    return img
def main():
    fname = '../images/gps.jpg'
    original = cv.imread(fname)
    cv.imshow("satellite image",original)
    img = smooth(original, 'bilateral')
    #plt.imshow(img)
    #plt.show()

   # get image dimensions
    xdim, ydim, nchannels = img.shape

    veg_to_background = detectve(img, xdim, ydim)

    segmented =mask(veg_to_background, xdim, ydim)

    detect = detectbui(segmented, original, xdim, ydim)

    # cv.imwrite(detect, '../images/summer2014/' + fname[:-4] + '-detect.png')

if __name__ == "__main__":
    main()