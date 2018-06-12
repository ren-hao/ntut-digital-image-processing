# coding: utf-8

import gc
import random
from os import listdir
from os.path import join

import numpy as np
from PIL import Image
from keras.layers import Dense, Conv2D, Activation, Dropout, Flatten, BatchNormalization
from keras.layers.advanced_activations import LeakyReLU
from keras.models import Sequential
from keras.optimizers import Adam

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
# print(img_list[1888])
# print(label_list[1888])


# RealY = np_utils.to_categorical(label_list,num_classes=len(model))
RealX = np.asarray(img_list)
RealY = np.array(label_list)
# RealY


# 鑑定器 D
# In: 64 x 64 x 3, depth = 3
modelD = Sequential()

modelD.add(Conv2D(filters=32, strides=2, kernel_size=(3, 3), padding='same', input_shape=(IMG_H, IMG_W, 3)))
modelD.add(BatchNormalization())
modelD.add(LeakyReLU(0.2))

modelD.add(Conv2D(filters=64, strides=2, kernel_size=(3, 3), padding='same'))
modelD.add(BatchNormalization())
modelD.add(LeakyReLU(0.2))

modelD.add(Conv2D(filters=128, strides=2, kernel_size=(3, 3), padding='same'))
modelD.add(BatchNormalization())
modelD.add(LeakyReLU(0.2))

modelD.add(Conv2D(filters=256, strides=2, kernel_size=(5, 5), padding='same'))
modelD.add(BatchNormalization())
modelD.add(LeakyReLU(0.2))

modelD.add(Conv2D(filters=512, strides=2, kernel_size=(5, 5), padding='same'))
modelD.add(BatchNormalization())
modelD.add(LeakyReLU(0.2))

optimizerD = Adam(0.0002, 0.5)
modelD.add(Dropout(0.2))
modelD.add(Flatten())
modelD.add(Dense(label_cnt))
modelD.add(Activation('softmax'))
modelD.compile(loss='binary_crossentropy', optimizer=optimizerD, metrics=['accuracy'])

modelD.summary()

modelD.fit(RealX, RealY, batch_size=256, epochs=20)

modelD.save("momey.h5")

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
