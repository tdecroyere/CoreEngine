#pragma once
#include "WindowsCommon.h"
#include "VulkanGraphicsService.h"

VkDeviceQueueCreateInfo CreateDeviceQueueCreateInfo(uint32_t queueFamilyIndex, uint32_t count)
{
    float* queuePriorities = new float[count];

    for (int i = 0; i < count; i++)
    {
        queuePriorities[i] = 1.0f;
    }

    VkDeviceQueueCreateInfo queueCreateInfo = { VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO };
    queueCreateInfo.pQueuePriorities = queuePriorities;
    queueCreateInfo.queueCount = count;
    queueCreateInfo.queueFamilyIndex = queueFamilyIndex;

    return queueCreateInfo;
}

VkFormat VulkanConvertTextureFormat(GraphicsTextureFormat textureFormat, bool noSrgb = false) 
{
	switch (textureFormat)
	{
		case GraphicsTextureFormat::Bgra8UnormSrgb:
			return noSrgb ? VK_FORMAT_B8G8R8A8_UNORM : VK_FORMAT_B8G8R8A8_SRGB;
	
		case GraphicsTextureFormat::Depth32Float:
			return VK_FORMAT_D32_SFLOAT;
	
		case GraphicsTextureFormat::Rgba16Float:
			return VK_FORMAT_R16G16B16A16_SFLOAT;
	
		case GraphicsTextureFormat::R16Float:
			return VK_FORMAT_R16_SFLOAT;
	
		case GraphicsTextureFormat::BC1Srgb:
			return VK_FORMAT_BC1_RGBA_SRGB_BLOCK;

		case GraphicsTextureFormat::BC2Srgb:
			return VK_FORMAT_BC2_SRGB_BLOCK;

		case GraphicsTextureFormat::BC3Srgb:
			return VK_FORMAT_BC3_SRGB_BLOCK;

		case GraphicsTextureFormat::BC4:
			return VK_FORMAT_BC4_UNORM_BLOCK;

		case GraphicsTextureFormat::BC5:
			return VK_FORMAT_BC5_UNORM_BLOCK;

		case GraphicsTextureFormat::BC6:
			return VK_FORMAT_BC6H_UFLOAT_BLOCK;

		case GraphicsTextureFormat::BC7Srgb:
			return VK_FORMAT_BC7_SRGB_BLOCK;

		case GraphicsTextureFormat::Rgba32Float:
			return VK_FORMAT_R32G32B32A32_SFLOAT;

		case GraphicsTextureFormat::Rgba16Unorm:
			return VK_FORMAT_R16G16B16A16_UNORM;
	}
        
	return VK_FORMAT_R8G8B8A8_SRGB;
}

VkImageView CreateImageView(VkDevice device, VkImage image, GraphicsTextureFormat textureFormat)
{
	VkImageViewCreateInfo createInfo = { VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO };
	createInfo.image = image;
	createInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
	createInfo.format = VulkanConvertTextureFormat(textureFormat, true);
	createInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
	createInfo.subresourceRange.levelCount = 1;
	createInfo.subresourceRange.layerCount = 1;

	VkImageView view = 0;
	AssertIfFailed(vkCreateImageView(device, &createInfo, 0, &view));

	return view;
}

VkFramebuffer CreateFramebuffer(VkDevice device, VkRenderPass renderPass, VkImageView imageView, uint32_t width, uint32_t height)
{
	VkFramebufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO };
	createInfo.renderPass = renderPass;
	createInfo.attachmentCount = 1;
	createInfo.pAttachments = &imageView;
	createInfo.width = width;
	createInfo.height = height;
	createInfo.layers = 1;

	VkFramebuffer framebuffer = nullptr;
	AssertIfFailed(vkCreateFramebuffer(device, &createInfo, 0, &framebuffer));

	return framebuffer;
}

VkShaderModule CreateShaderModule(VkDevice device, void* data, int dataLength)
{
	VkShaderModuleCreateInfo createInfo = { VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO };
	createInfo.codeSize = dataLength;
	createInfo.pCode = reinterpret_cast<const uint32_t*>(data);

	VkShaderModule shaderModule = nullptr;
	AssertIfFailed(vkCreateShaderModule(device, &createInfo, 0, &shaderModule));

	return shaderModule;
}