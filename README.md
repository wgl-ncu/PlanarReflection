# Planar Reflection

平面反射

渲染阶段：

1. 可以利用将相机设置到与平面对称的地方或者使用反射矩阵来完成反射画面的渲染，渲染结果保存到一张贴图中，此次渲染可以采用更简单的光照模型、面数更低的模型等毕竟不需要特别清晰

![](https://typora-picture-back-up.oss-cn-hangzhou.aliyuncs.com/pr2.png)

采样阶段：

将平面反射加入到间接光的镜面反射中去，项目中直接修改了Lit shader实现的pbr流程

![](https://typora-picture-back-up.oss-cn-hangzhou.aliyuncs.com/pr4.png)

效果：

![](https://typora-picture-back-up.oss-cn-hangzhou.aliyuncs.com/pr3.png)

![](https://typora-picture-back-up.oss-cn-hangzhou.aliyuncs.com/pr5.png)
