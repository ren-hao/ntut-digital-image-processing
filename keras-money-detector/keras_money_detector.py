# coding: utf-8

import gc
import random
from os import listdir
from os.path import join

import numpy as np
from PIL import Image
from keras.layers import Dense, Conv2D, Flatten, MaxPooling2D
from keras.models import Sequential

IMG_W, IMG_H = 360, 202
model_label = ["100", "1000", "N"]


def load_img(i_path: str):
    img = Image.open(i_path)
    img = img.resize((IMG_W, IMG_H), Image.ANTIALIAS)
    img = np.array(img)
    o = np.ones(img.shape)
    img = img - (o * 127.5)
    img = img / 127.5
    return img


def get_label(p_arr):
    global model_label
    if len(p_arr.shape) == 1:
        cond = p_arr >= 0.9
        for i in range(0, p_arr.shape[0]):
            if cond[i]:
                return model_label[i]

        return model_label[-1]
    else:
        return list(map(lambda x: get_label(x), p_arr))


label = 0
label_cnt = len(model_label) - 1
img_list = []
label_list = []

for path in model_label:
    files = listdir(path)
    for f in files:
        full_path = join(path, f)
        img = load_img(full_path)
        img_list.append(img)
        if path == "N":
            label_list.append([0] * label_cnt)
        else:
            tmp = [0] * label_cnt
            tmp[label] = 1
            label_list.append(tmp)

    label += 1

combined = list(zip(img_list, label_list))
random.shuffle(combined)
img_list[:], label_list[:] = zip(*combined)
gc.collect()

RealX = np.asarray(img_list)
RealY = np.array(label_list)

# 鑑定器 D
# In: 64 x 64 x 3, depth = 3
modelD = Sequential()

modelD.add(Conv2D(filters=16, kernel_size=(3, 3), padding='same', input_shape=(IMG_H, IMG_W, 3)))
modelD.add(MaxPooling2D(pool_size=(2, 2)))
# modelD.add(Dropout(0.2))

modelD.add(Conv2D(filters=32, kernel_size=(3, 3), padding='same', activation='relu'))
modelD.add(MaxPooling2D(pool_size=(2, 2)))

modelD.add(Conv2D(filters=64, kernel_size=(5, 5), padding='same', activation='relu'))
modelD.add(MaxPooling2D(pool_size=(2, 2)))

modelD.add(Conv2D(filters=128, kernel_size=(5, 5), padding='same', activation='relu'))
modelD.add(MaxPooling2D(pool_size=(2, 2)))

modelD.add(Flatten())

modelD.add(Dense(1024, activation='relu'))

modelD.add(Dense(label_cnt, activation='sigmoid'))

modelD.compile(loss='binary_crossentropy', optimizer='adam', metrics=['accuracy'])

modelD.summary()

modelD.fit(RealX, RealY, batch_size=130, epochs=20)

modelD.save("money3.h5")

"""
test
"""
gc.collect()
test_files = listdir("test")
test_x = []

for f in test_files:
    fullpath = join("test", f)
    img = load_img(fullpath)
    test_x.append(img)

test_p_arr = modelD.predict(np.asarray(test_x))
p_result = get_label(test_p_arr)
print(list(zip(test_files, p_result)))
print(test_p_arr)
