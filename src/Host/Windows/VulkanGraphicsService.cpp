#pragma once
#include "WindowsCommon.h"
#include "VulkanGraphicsService.h"
#include "VulkanGraphicsServiceUtils.h"

VulkanGraphicsService::VulkanGraphicsService()
{
    this->vulkanInstance = CreateVulkanInstance();

    this->graphicsPhysicalDevice = FindGraphicsDevice();
    
    this->graphicsDevice = CreateDevice(this->graphicsPhysicalDevice);

    #ifdef DEBUG
    RegisterDebugCallback();
    #endif
}

VulkanGraphicsService::~VulkanGraphicsService()
{
    if (this->graphicsDevice != nullptr)
    {
        for (int i = 0; i < this->frameBuffersToDelete.size(); i++)
        {
            vkDestroyFramebuffer(this->graphicsDevice, this->frameBuffersToDelete[i], nullptr);
        }

        vkDestroyDevice(this->graphicsDevice, nullptr);
    }

    if (this->debugCallback != nullptr)
    {
        vkDestroyDebugReportCallback(this->vulkanInstance, this->debugCallback, nullptr);
    }

    if (this->vulkanInstance != nullptr)
    {
        vkDestroyInstance(this->vulkanInstance, nullptr);
    }
}

void VulkanGraphicsService::GetGraphicsAdapterName(char* output)
{
    this->deviceName.copy((wchar_t*)output, this->deviceName.length());
}

GraphicsAllocationInfos VulkanGraphicsService::GetBufferAllocationInfos(int sizeInBytes)
{
	VkBufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO };
    createInfo.size = sizeInBytes;
    createInfo.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;

    VkBuffer buffer = nullptr;
    AssertIfFailed(vkCreateBuffer(this->graphicsDevice, &createInfo, nullptr, &buffer));

    VkMemoryRequirements memoryRequirements = {};
    vkGetBufferMemoryRequirements(this->graphicsDevice, buffer, &memoryRequirements);

    GraphicsAllocationInfos result = {};
	result.SizeInBytes = memoryRequirements.size;
	result.Alignment = memoryRequirements.alignment;

    vkDestroyBuffer(this->graphicsDevice, buffer, nullptr);

	return result;
}

GraphicsAllocationInfos VulkanGraphicsService::GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    // TODO: Avoid the creation of an image here
    VkImage tmpImage = CreateImage(this->graphicsDevice, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);

    VkMemoryRequirements memoryRequirements = {};
    vkGetImageMemoryRequirements(this->graphicsDevice, tmpImage, &memoryRequirements);

    vkDestroyImage(this->graphicsDevice, tmpImage, nullptr);

    GraphicsAllocationInfos result = {};
	result.SizeInBytes = memoryRequirements.size;
	result.Alignment = memoryRequirements.alignment;
	return result;
}

void* VulkanGraphicsService::CreateCommandQueue(enum GraphicsServiceCommandType commandQueueType)
{
    VulkanCommandQueue* commandQueue = new VulkanCommandQueue();

    uint32_t queueFamilyIndex = this->renderCommandQueueFamilyIndex;

    if (commandQueueType == GraphicsServiceCommandType::Compute)
    {
        queueFamilyIndex = this->computeCommandQueueFamilyIndex;
    }

    else if (commandQueueType == GraphicsServiceCommandType::Copy)
    {
        queueFamilyIndex = this->copyCommandQueueFamilyIndex;
    }

    commandQueue->CommandQueueFamilyIndex = queueFamilyIndex;

    vkGetDeviceQueue(this->graphicsDevice, queueFamilyIndex, 0, &commandQueue->CommandQueueObject);

	// Init command allocators for each frame in flight
	// TODO: For multi threading support we need to allocate on allocator per frame per thread
	for (int i = 0; i < VulkanFramesCount; i++)
	{
        VkCommandPoolCreateInfo createInfo = { VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO };
        createInfo.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT ;
        createInfo.queueFamilyIndex = queueFamilyIndex;

        VkCommandPool commandPool = 0;
        AssertIfFailed(vkCreateCommandPool(this->graphicsDevice, &createInfo, 0, &commandPool));

		commandQueue->CommandPools[i] = commandPool;
	}

    VkSemaphoreTypeCreateInfo timelineCreateInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_TYPE_CREATE_INFO };
    timelineCreateInfo.semaphoreType = VK_SEMAPHORE_TYPE_TIMELINE;
    timelineCreateInfo.initialValue = 0;

    VkSemaphoreCreateInfo createInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO };
    createInfo.pNext = &timelineCreateInfo;

    AssertIfFailed(vkCreateSemaphore(this->graphicsDevice, &createInfo, NULL, &commandQueue->TimelineSemaphore));

    return commandQueue;
}

void VulkanGraphicsService::SetCommandQueueLabel(void* commandQueuePointer, char* label)
{
    #ifdef DEBUG 
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;

    for (int i = 0; i < VulkanFramesCount; i++)
	{
        VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
        nameInfo.objectType = VK_OBJECT_TYPE_COMMAND_POOL;
        nameInfo.objectHandle = (uint64_t)commandQueue->CommandPools[i];
        nameInfo.pObjectName = label;

        AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    }
    #endif
}
void VulkanGraphicsService::DeleteCommandQueue(void* commandQueuePointer)
{ 
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;
    
    for (int i = 0; i < VulkanFramesCount; i++)
    {
        vkDestroyCommandPool(this->graphicsDevice, commandQueue->CommandPools[i], nullptr);
    }

    vkDestroySemaphore(this->graphicsDevice, commandQueue->TimelineSemaphore, nullptr);

    delete commandQueue;
}

void VulkanGraphicsService::ResetCommandQueue(void* commandQueuePointer)
{
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;

    auto commandPool = commandQueue->CommandPools[this->currentCommandPoolIndex];
    AssertIfFailed(vkResetCommandPool(this->graphicsDevice, commandPool, 0));
}

unsigned long VulkanGraphicsService::GetCommandQueueTimestampFrequency(void* commandQueuePointer)
{
    VkPhysicalDeviceProperties properties;
    vkGetPhysicalDeviceProperties(this->graphicsPhysicalDevice, &properties);

    return (unsigned long)properties.limits.timestampPeriod * 1000000000;
}

unsigned long VulkanGraphicsService::ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength)
{
    // TODO: Reuse already allocated arrays
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;

    VkPipelineStageFlags* submitStageMasks = new VkPipelineStageFlags[fencesToWaitLength];
    VkSemaphore* waitSemaphores = new VkSemaphore[fencesToWaitLength];
    uint64_t* waitSemaphoreValues = new uint64_t[fencesToWaitLength];

    for (int i = 0; i < fencesToWaitLength; i++)
	{
		auto fenceToWait = fencesToWait[i];
		VulkanCommandQueue* commandQueueToWait = (VulkanCommandQueue*)fenceToWait.CommandQueuePointer;

        waitSemaphores[i] = commandQueueToWait->TimelineSemaphore;
        waitSemaphoreValues[i] = commandQueueToWait->FenceValue;
        submitStageMasks[i] = VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT;
	}

    VkCommandBuffer* vulkanCommandBuffers = new VkCommandBuffer[commandListsLength];

    for (int i = 0; i < commandListsLength; i++)
	{
		VulkanCommandList* vulkanCommandList = (VulkanCommandList*)commandLists[i];
        vulkanCommandBuffers[i] = vulkanCommandList->CommandBufferObject;
	}

    const uint64_t signalValue = commandQueue->FenceValue + 1;
	commandQueue->FenceValue = signalValue;

    VkTimelineSemaphoreSubmitInfo timelineInfo = { VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO };
    timelineInfo.waitSemaphoreValueCount = fencesToWaitLength;
    timelineInfo.pWaitSemaphoreValues = waitSemaphoreValues;
    timelineInfo.signalSemaphoreValueCount = 1;
    timelineInfo.pSignalSemaphoreValues = &signalValue;

    VkSubmitInfo submitInfo = { VK_STRUCTURE_TYPE_SUBMIT_INFO };
    submitInfo.pNext = &timelineInfo;
    submitInfo.waitSemaphoreCount = fencesToWaitLength;
    submitInfo.pWaitSemaphores = waitSemaphores;
    submitInfo.signalSemaphoreCount = 1;
    submitInfo.pSignalSemaphores = &commandQueue->TimelineSemaphore;
    submitInfo.commandBufferCount = commandListsLength;
    submitInfo.pCommandBuffers = vulkanCommandBuffers;
    submitInfo.pWaitDstStageMask = submitStageMasks;

    AssertIfFailed(vkQueueSubmit(commandQueue->CommandQueueObject, 1, &submitInfo, VK_NULL_HANDLE));

    delete submitStageMasks;
    delete waitSemaphores;
    delete waitSemaphoreValues;
    delete vulkanCommandBuffers;

    return signalValue;
}

void VulkanGraphicsService::WaitForCommandQueueOnCpu(struct GraphicsFence fenceToWait)
{
    VulkanCommandQueue* commandQueueToWait = (VulkanCommandQueue*)fenceToWait.CommandQueuePointer;
    const uint64_t waitValue = fenceToWait.Value;

    VkSemaphoreWaitInfo waitInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO };
    waitInfo.semaphoreCount = 1;
    waitInfo.pSemaphores = &commandQueueToWait->TimelineSemaphore;
    waitInfo.pValues = &waitValue;

    AssertIfFailed(vkWaitSemaphores(this->graphicsDevice, &waitInfo, UINT64_MAX));
}

void* VulkanGraphicsService::CreateCommandList(void* commandQueuePointer)
{
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;

	auto commandPool = commandQueue->CommandPools[this->currentCommandPoolIndex];

    VkCommandBufferAllocateInfo allocateInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
	allocateInfo.commandPool = commandPool;
	allocateInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
	allocateInfo.commandBufferCount = 1;

	VkCommandBuffer commandBuffer = 0;
	AssertIfFailed(vkAllocateCommandBuffers(this->graphicsDevice, &allocateInfo, &commandBuffer));

    VkCommandBufferBeginInfo beginInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO };
    beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

    AssertIfFailed(vkBeginCommandBuffer(commandBuffer, &beginInfo));

    VulkanCommandList* commandList = new VulkanCommandList();
	commandList->CommandBufferObject = commandBuffer;
	commandList->CommandQueue = commandQueue;

    return commandList;
}

void VulkanGraphicsService::SetCommandListLabel(void* commandListPointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;

    VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
    nameInfo.objectType = VK_OBJECT_TYPE_COMMAND_BUFFER;
    nameInfo.objectHandle = (uint64_t)commandList->CommandBufferObject;
    nameInfo.pObjectName = label;

    AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    #endif
}

void VulkanGraphicsService::DeleteCommandList(void* commandListPointer)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    delete commandList;
}

void VulkanGraphicsService::ResetCommandList(void* commandListPointer)
{
    // TODO: It seems we cannot reset the command buffer directly after submition like in D3D12
    // Confirm that

    // TODO: Check how to delete the old command buffer object because it can still be in use

    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;

    // AssertIfFailed(vkResetCommandBuffer(commandList->CommandBufferObject, 0));

    auto commandPool = commandList->CommandQueue->CommandPools[this->currentCommandPoolIndex];

    VkCommandBufferAllocateInfo allocateInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
	allocateInfo.commandPool = commandPool;
	allocateInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
	allocateInfo.commandBufferCount = 1;

	VkCommandBuffer commandBuffer = 0;
	AssertIfFailed(vkAllocateCommandBuffers(this->graphicsDevice, &allocateInfo, &commandBuffer));
    commandList->CommandBufferObject = commandBuffer;

    VkCommandBufferBeginInfo beginInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO };
    beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

    AssertIfFailed(vkBeginCommandBuffer(commandList->CommandBufferObject, &beginInfo));
}

void VulkanGraphicsService::CommitCommandList(void* commandListPointer)
{
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    AssertIfFailed(vkEndCommandBuffer(commandList->CommandBufferObject));
}

void* VulkanGraphicsService::CreateGraphicsHeap(enum GraphicsServiceHeapType type, unsigned long sizeInBytes)
{
    VkMemoryAllocateInfo allocateInfo = { VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO };
    allocateInfo.allocationSize = sizeInBytes;

    if (type == GraphicsServiceHeapType::Gpu)
    {
        allocateInfo.memoryTypeIndex = this->gpuMemoryTypeIndex;
    }

    else if (type == GraphicsServiceHeapType::Upload)
    {
        allocateInfo.memoryTypeIndex = this->uploadMemoryTypeIndex;
    }

    else if (type == GraphicsServiceHeapType::ReadBack)
    {
        allocateInfo.memoryTypeIndex = this->readBackMemoryTypeIndex;
    }

    VulkanGraphicsHeap* graphicsHeap = new VulkanGraphicsHeap();
    graphicsHeap->Type = type;

    AssertIfFailed(vkAllocateMemory(this->graphicsDevice, &allocateInfo, nullptr, &graphicsHeap->DeviceMemory));

    return graphicsHeap;
}

void VulkanGraphicsService::SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanGraphicsHeap* graphicsHeap = (VulkanGraphicsHeap*)graphicsHeapPointer;

    VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
    nameInfo.objectType = VK_OBJECT_TYPE_DEVICE_MEMORY;
    nameInfo.objectHandle = (uint64_t)graphicsHeap->DeviceMemory;
    nameInfo.pObjectName = label;

    AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    #endif
}

void VulkanGraphicsService::DeleteGraphicsHeap(void* graphicsHeapPointer)
{ 
    VulkanGraphicsHeap* graphicsHeap = (VulkanGraphicsHeap*)graphicsHeapPointer;
    vkFreeMemory(this->graphicsDevice, graphicsHeap->DeviceMemory, nullptr);

    delete graphicsHeap;
}

void* VulkanGraphicsService::CreateShaderResourceHeap(unsigned long length)
{
    VulkanShaderResourceHeap* resourceHeap = new VulkanShaderResourceHeap();

    VkDescriptorPoolSize poolSizes[]
    {
        {VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, length},
        {VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, length },
        {VK_DESCRIPTOR_TYPE_SAMPLER, 1 }
    };

    VkDescriptorPoolCreateInfo createInfo = { VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO };
    createInfo.poolSizeCount = ARRAYSIZE(poolSizes);
    createInfo.pPoolSizes = poolSizes;
    createInfo.flags = VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT;
    createInfo.maxSets = 3;

    vkCreateDescriptorPool(this->graphicsDevice, &createInfo, nullptr, &resourceHeap->DescriptorPool);

    VkDescriptorSetLayout setLayouts[] {
        GetGlobalBufferLayout(this->graphicsDevice),
	    GetGlobalTextureLayout(this->graphicsDevice),
	    GetGlobalSamplerLayout(this->graphicsDevice)
    };

    uint32_t counts[] { length, length, 1 };

    VkDescriptorSetVariableDescriptorCountAllocateInfo set_counts = {};
    set_counts.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_VARIABLE_DESCRIPTOR_COUNT_ALLOCATE_INFO;
    set_counts.descriptorSetCount = ARRAYSIZE(counts);
    set_counts.pDescriptorCounts = counts;
    
    VkDescriptorSetAllocateInfo allocateInfo = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO };
    allocateInfo.pSetLayouts = setLayouts;
    allocateInfo.descriptorSetCount = ARRAYSIZE(setLayouts);
    allocateInfo.descriptorPool = resourceHeap->DescriptorPool;
    allocateInfo.pNext = &set_counts;

    vkAllocateDescriptorSets(this->graphicsDevice, &allocateInfo, resourceHeap->DescriptorSets);

    // TODO: Don't allocate the sampler like that
    VkSamplerCreateInfo samplerCreateInfo = { VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO };
    samplerCreateInfo.mipmapMode = VK_SAMPLER_MIPMAP_MODE_NEAREST;
    samplerCreateInfo.minFilter = VK_FILTER_NEAREST;
    samplerCreateInfo.magFilter = VK_FILTER_NEAREST;

    AssertIfFailed(vkCreateSampler(this->graphicsDevice, &samplerCreateInfo, nullptr, &resourceHeap->Sampler));

    VkDescriptorImageInfo samplerInfo = {};
    samplerInfo.sampler = resourceHeap->Sampler;

    VkWriteDescriptorSet descriptor = { VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET };
    descriptor.dstSet = resourceHeap->DescriptorSets[2];
    descriptor.dstBinding = 0;
    descriptor.dstArrayElement = 0;
    descriptor.descriptorCount = 1;
    descriptor.descriptorType = VK_DESCRIPTOR_TYPE_SAMPLER;
    descriptor.pImageInfo = &samplerInfo;

    vkUpdateDescriptorSets(this->graphicsDevice, 1, &descriptor, 0, nullptr);

    return resourceHeap;
}

void VulkanGraphicsService::SetShaderResourceHeapLabel(void* shaderResourceHeapPointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanShaderResourceHeap* shaderResourceHeap = (VulkanShaderResourceHeap*)shaderResourceHeapPointer;

    VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
    nameInfo.objectType = VK_OBJECT_TYPE_DESCRIPTOR_POOL;
    nameInfo.objectHandle = (uint64_t)shaderResourceHeap->DescriptorPool;
    nameInfo.pObjectName = label;

    AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    #endif
}

void VulkanGraphicsService::DeleteShaderResourceHeap(void* shaderResourceHeapPointer)
{ 
    VulkanShaderResourceHeap* shaderResourceHeap = (VulkanShaderResourceHeap*)shaderResourceHeapPointer;
    vkDestroyDescriptorPool(this->graphicsDevice, shaderResourceHeap->DescriptorPool, nullptr);

    // TODO: Remove that workaround
    vkDestroySampler(this->graphicsDevice, shaderResourceHeap->Sampler, nullptr);
    vkDestroyDescriptorSetLayout(this->graphicsDevice, globalBufferLayout, nullptr);
    vkDestroyDescriptorSetLayout(this->graphicsDevice, globalTextureLayout, nullptr);
    vkDestroyDescriptorSetLayout(this->graphicsDevice, globalSamplerLayout, nullptr);

    delete shaderResourceHeap;
}

void VulkanGraphicsService::CreateShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index, void* texturePointer)
{ 
    VulkanShaderResourceHeap* shaderResourceHeap = (VulkanShaderResourceHeap*)shaderResourceHeapPointer;
    VulkanTexture* texture = (VulkanTexture*)texturePointer;

    VkDescriptorImageInfo imageInfo = {};
    imageInfo.imageView = texture->ImageView;
    imageInfo.imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;

    VkWriteDescriptorSet descriptor = { VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET };
    descriptor.dstSet = shaderResourceHeap->DescriptorSets[1];
    descriptor.dstBinding = 0;
    descriptor.dstArrayElement = index;
    descriptor.descriptorCount = 1;
    descriptor.descriptorType = VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
    descriptor.pImageInfo = &imageInfo;

    vkUpdateDescriptorSets(this->graphicsDevice, 1, &descriptor, 0, nullptr);
}

void VulkanGraphicsService::DeleteShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index){ }

void VulkanGraphicsService::CreateShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer)
{ 
    VulkanShaderResourceHeap* shaderResourceHeap = (VulkanShaderResourceHeap*)shaderResourceHeapPointer;
    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)bufferPointer;

    VkDescriptorBufferInfo bufferInfo = {};
    bufferInfo.buffer = graphicsBuffer->BufferObject;
    bufferInfo.range = graphicsBuffer->SizeInBytes;

    VkWriteDescriptorSet descriptor = { VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET };
    descriptor.dstSet = shaderResourceHeap->DescriptorSets[0];
    descriptor.dstBinding = 0;
    descriptor.dstArrayElement = index;
    descriptor.descriptorCount = 1;
    descriptor.descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
    descriptor.pBufferInfo = &bufferInfo;

    vkUpdateDescriptorSets(this->graphicsDevice, 1, &descriptor, 0, nullptr);
}

void VulkanGraphicsService::DeleteShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index){ }

void* VulkanGraphicsService::CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{
    VulkanGraphicsHeap* graphicsHeap = (VulkanGraphicsHeap*)graphicsHeapPointer;
    VulkanGraphicsBuffer* graphicsBuffer = new VulkanGraphicsBuffer();
    graphicsBuffer->SizeInBytes = sizeInBytes;
    graphicsBuffer->HeapOffset = heapOffset;
    graphicsBuffer->GraphicsHeap = graphicsHeap;

    VkBufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO };
    createInfo.size = sizeInBytes;
    createInfo.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT; // TODO: To fill correctly base on the heap type

    if (graphicsHeap->Type == GraphicsServiceHeapType::Gpu)
	{
		createInfo.usage |= VK_BUFFER_USAGE_TRANSFER_DST_BIT;
	}

    else if (graphicsHeap->Type == GraphicsServiceHeapType::Upload)
	{
		createInfo.usage |= VK_BUFFER_USAGE_TRANSFER_SRC_BIT;
	}

    else if (graphicsHeap->Type == GraphicsServiceHeapType::ReadBack)
    {
        createInfo.usage |= VK_BUFFER_USAGE_TRANSFER_DST_BIT;
    }

    AssertIfFailed(vkCreateBuffer(this->graphicsDevice, &createInfo, nullptr, &graphicsBuffer->BufferObject));
    AssertIfFailed(vkBindBufferMemory(this->graphicsDevice, graphicsBuffer->BufferObject, graphicsHeap->DeviceMemory, heapOffset));

    return graphicsBuffer;
}

void VulkanGraphicsService::SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)graphicsBufferPointer;

    VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
    nameInfo.objectType = VK_OBJECT_TYPE_BUFFER;
    nameInfo.objectHandle = (uint64_t)graphicsBuffer->BufferObject;
    nameInfo.pObjectName = label;

    AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    #endif
}

void VulkanGraphicsService::DeleteGraphicsBuffer(void* graphicsBufferPointer)
{ 
    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)graphicsBufferPointer;
    vkDestroyBuffer(this->graphicsDevice, graphicsBuffer->BufferObject, nullptr);

    delete graphicsBuffer;
}

void* VulkanGraphicsService::GetGraphicsBufferCpuPointer(void* graphicsBufferPointer)
{
    // TODO: Only get a pointer for a portion of the DeviceMemory

    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)graphicsBufferPointer;

    if (graphicsBuffer->CpuPointer == nullptr)
    {
        AssertIfFailed(vkMapMemory(this->graphicsDevice, graphicsBuffer->GraphicsHeap->DeviceMemory, graphicsBuffer->HeapOffset, graphicsBuffer->SizeInBytes, 0, &graphicsBuffer->CpuPointer));
    }

    return graphicsBuffer->CpuPointer;
}

void VulkanGraphicsService::ReleaseGraphicsBufferCpuPointer(void* graphicsBufferPointer)
{
    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)graphicsBufferPointer;

    if (graphicsBuffer->CpuPointer != nullptr)
    {
        vkUnmapMemory(this->graphicsDevice, graphicsBuffer->GraphicsHeap->DeviceMemory);
        graphicsBuffer->CpuPointer = nullptr;
    }
}

void* VulkanGraphicsService::CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    VulkanGraphicsHeap* graphicsHeap = (VulkanGraphicsHeap*)graphicsHeapPointer;
    VulkanTexture* texture = new VulkanTexture();

    texture->TextureObject = CreateImage(this->graphicsDevice, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
    AssertIfFailed(vkBindImageMemory(this->graphicsDevice, texture->TextureObject, graphicsHeap->DeviceMemory, heapOffset));

    texture->ImageView = CreateImageView(this->graphicsDevice, texture->TextureObject, VulkanConvertTextureFormat(textureFormat));
    texture->Width = width;
    texture->Height = height;
    texture->ResourceState = VK_IMAGE_LAYOUT_UNDEFINED;
    texture->Format = VulkanConvertTextureFormat(textureFormat);

    return texture;
}

void VulkanGraphicsService::SetTextureLabel(void* texturePointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanTexture* texture = (VulkanTexture*)texturePointer;

    VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
    nameInfo.objectType = VK_OBJECT_TYPE_IMAGE;
    nameInfo.objectHandle = (uint64_t)texture->TextureObject;
    nameInfo.pObjectName = label;

    AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    #endif
}

void VulkanGraphicsService::DeleteTexture(void* texturePointer)
{ 
    VulkanTexture* texture = (VulkanTexture*)texturePointer;
    vkDestroyImageView(this->graphicsDevice, texture->ImageView, nullptr);

    if (!texture->IsPresentTexture)
    {
        vkDestroyImage(this->graphicsDevice, texture->TextureObject, nullptr);
    }

    delete texture;    
}

void* VulkanGraphicsService::CreateSwapChain(void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
    VulkanSwapChain* swapChain = new VulkanSwapChain();
    swapChain->CommandQueue = (VulkanCommandQueue*)commandQueuePointer;

    VkWin32SurfaceCreateInfoKHR surfaceCreateInfo = { VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR };
    surfaceCreateInfo.hinstance = GetModuleHandle(nullptr);
    surfaceCreateInfo.hwnd = (HWND)windowPointer;

    AssertIfFailed(vkCreateWin32SurfaceKHR(this->vulkanInstance, &surfaceCreateInfo, nullptr, &swapChain->WindowSurface));

    VkBool32 isPresentSupported;
    AssertIfFailed(vkGetPhysicalDeviceSurfaceSupportKHR(this->graphicsPhysicalDevice, swapChain->CommandQueue->CommandQueueFamilyIndex, swapChain->WindowSurface, &isPresentSupported));
    assert(isPresentSupported == 1);

    VkSurfaceCapabilitiesKHR surfaceCapabilities;
    AssertIfFailed(vkGetPhysicalDeviceSurfaceCapabilitiesKHR(this->graphicsPhysicalDevice, swapChain->WindowSurface, &surfaceCapabilities));

    swapChain->Format = VulkanConvertTextureFormat(textureFormat, true);

    VkFormat formatList[] = 
    {
        VulkanConvertTextureFormat(textureFormat, true),
        VulkanConvertTextureFormat(textureFormat)
    };

    VkImageFormatListCreateInfo imageFormatListCreateInfo = { VK_STRUCTURE_TYPE_IMAGE_FORMAT_LIST_CREATE_INFO };
    imageFormatListCreateInfo.pViewFormats = formatList;
    imageFormatListCreateInfo.viewFormatCount = ARRAYSIZE(formatList);

    VkSwapchainCreateInfoKHR swapChainCreateInfo = { VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR };
    swapChainCreateInfo.surface = swapChain->WindowSurface;
    swapChainCreateInfo.minImageCount = VulkanFramesCount;
    swapChainCreateInfo.imageFormat = swapChain->Format;
    swapChainCreateInfo.imageColorSpace = VK_COLOR_SPACE_SRGB_NONLINEAR_KHR;
    swapChainCreateInfo.imageExtent.width = width;
    swapChainCreateInfo.imageExtent.height = height;
    swapChainCreateInfo.imageArrayLayers = 1;
    swapChainCreateInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    swapChainCreateInfo.presentMode = VK_PRESENT_MODE_FIFO_KHR;
    swapChainCreateInfo.preTransform = surfaceCapabilities.currentTransform;
    swapChainCreateInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
    swapChainCreateInfo.flags = VK_SWAPCHAIN_CREATE_MUTABLE_FORMAT_BIT_KHR;
    swapChainCreateInfo.pNext = &imageFormatListCreateInfo;

    AssertIfFailed(vkCreateSwapchainKHR(this->graphicsDevice, &swapChainCreateInfo, nullptr, &swapChain->SwapChainObject));

	uint32_t swapchainImageCount = 0;
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, nullptr));
	
    vector<VkImage> swapchainImages(VulkanFramesCount);
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, swapchainImages.data()));

    for (int i = 0; i < swapchainImageCount; i++)
    {
        VulkanTexture* backBufferTexture = new VulkanTexture();
        backBufferTexture->TextureObject = swapchainImages[i];
        backBufferTexture->IsPresentTexture = true;
        backBufferTexture->ImageView = CreateImageView(this->graphicsDevice, swapchainImages[i], VulkanConvertTextureFormat(textureFormat));
        backBufferTexture->Width = width;
        backBufferTexture->Height = height;
        backBufferTexture->ResourceState = VK_IMAGE_LAYOUT_UNDEFINED;
        backBufferTexture->Format = VulkanConvertTextureFormat(textureFormat);

        swapChain->BackBufferTextures[i] = backBufferTexture;
    }

    swapChain->BackBufferAcquireFence = VulkanCreateFence(this->graphicsDevice);
    return swapChain;
}

void VulkanGraphicsService::DeleteSwapChain(void* swapChainPointer)
{
	VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;
    vkDestroyFence(this->graphicsDevice, swapChain->BackBufferAcquireFence, nullptr);

    for (int i = 0; i < VulkanFramesCount; i++)
    {
        DeleteTexture(swapChain->BackBufferTextures[i]);
    }
	
    delete swapChain;
}

void VulkanGraphicsService::ResizeSwapChain(void* swapChainPointer, int width, int height)
{
    AssertIfFailed(vkDeviceWaitIdle(this->graphicsDevice));

    VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;

    VkSwapchainKHR oldSwapchain = swapChain->SwapChainObject;

    VkFormat formatList[] = 
    {
        swapChain->Format,
        swapChain->BackBufferTextures[0]->Format
    };

    // TODO: Create an util function for this
    VkImageFormatListCreateInfo imageFormatListCreateInfo = { VK_STRUCTURE_TYPE_IMAGE_FORMAT_LIST_CREATE_INFO };
    imageFormatListCreateInfo.pViewFormats = formatList;
    imageFormatListCreateInfo.viewFormatCount = ARRAYSIZE(formatList);

    VkSwapchainCreateInfoKHR swapChainCreateInfo = { VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR };
    swapChainCreateInfo.surface = swapChain->WindowSurface;
    swapChainCreateInfo.minImageCount = VulkanFramesCount;
    swapChainCreateInfo.imageFormat = swapChain->Format;
    swapChainCreateInfo.imageColorSpace = VK_COLOR_SPACE_SRGB_NONLINEAR_KHR;
    swapChainCreateInfo.imageExtent.width = width;
    swapChainCreateInfo.imageExtent.height = height;
    swapChainCreateInfo.imageArrayLayers = 1;
    swapChainCreateInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    swapChainCreateInfo.presentMode = VK_PRESENT_MODE_FIFO_KHR;
    swapChainCreateInfo.preTransform = VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR;
    swapChainCreateInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
    swapChainCreateInfo.oldSwapchain = oldSwapchain;
    swapChainCreateInfo.flags = VK_SWAPCHAIN_CREATE_MUTABLE_FORMAT_BIT_KHR;
    swapChainCreateInfo.pNext = &imageFormatListCreateInfo;
    
    AssertIfFailed(vkCreateSwapchainKHR(this->graphicsDevice, &swapChainCreateInfo, nullptr, &swapChain->SwapChainObject));

	uint32_t swapchainImageCount = 0;
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, nullptr));
	
    vector<VkImage> swapchainImages(VulkanFramesCount);
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, swapchainImages.data()));

    for (int i = 0; i < swapchainImageCount; i++)
    {
        VkFormat oldFormat = swapChain->BackBufferTextures[i]->Format;

        DeleteTexture(swapChain->BackBufferTextures[i]);

        VulkanTexture* backBufferTexture = new VulkanTexture();
        backBufferTexture->TextureObject = swapchainImages[i];
        backBufferTexture->IsPresentTexture = true;
        backBufferTexture->ImageView = CreateImageView(this->graphicsDevice, swapchainImages[i], oldFormat);
        backBufferTexture->Width = width;
        backBufferTexture->Height = height;
        backBufferTexture->ResourceState = VK_IMAGE_LAYOUT_UNDEFINED;
        backBufferTexture->Format = oldFormat;

        swapChain->BackBufferTextures[i] = backBufferTexture;
    }

    vkDestroySwapchainKHR(this->graphicsDevice, oldSwapchain, nullptr);

    WaitForSwapChainOnCpu(swapChainPointer);
}

void* VulkanGraphicsService::GetSwapChainBackBufferTexture(void* swapChainPointer)
{
    VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;
    return swapChain->BackBufferTextures[swapChain->CurrentImageIndex];
}

unsigned long VulkanGraphicsService::PresentSwapChain(void* swapChainPointer)
{
    // TODO: Wait for the correct timeline semaphore value?
    // Or just issue a barrier because the final buffer rendering and the present is done on the same queue
    VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;

    VkPresentInfoKHR presentInfo = { VK_STRUCTURE_TYPE_PRESENT_INFO_KHR };
    presentInfo.waitSemaphoreCount = 0;
    // presentInfo.pWaitSemaphores = &releaseSemaphore;
    presentInfo.swapchainCount = 1;
    presentInfo.pSwapchains = &swapChain->SwapChainObject;
    presentInfo.pImageIndices = &swapChain->CurrentImageIndex;

    AssertIfFailed(vkQueuePresentKHR(swapChain->CommandQueue->CommandQueueObject, &presentInfo));

    // TODO: Return fence value

    // TODO: Do something better here
	this->currentCommandPoolIndex = (this->currentCommandPoolIndex + 1) % VulkanFramesCount;

    return 0;
}

void VulkanGraphicsService::WaitForSwapChainOnCpu(void* swapChainPointer)
{
    // TODO: Do something better here and try to emulate the SetLatency awaitable of D3D12
    VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;

    if (swapChain->CommandQueue->FenceValue > 0)
    {
        uint64_t fenceValue = swapChain->CommandQueue->FenceValue;

        VkSemaphoreWaitInfo waitInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO };
        waitInfo.pSemaphores = &swapChain->CommandQueue->TimelineSemaphore;
        waitInfo.pValues = &fenceValue;
        waitInfo.semaphoreCount = 1;

        vkWaitSemaphores(this->graphicsDevice, &waitInfo, UINT64_MAX);
    }

	AssertIfFailed(vkAcquireNextImageKHR(this->graphicsDevice, swapChain->SwapChainObject, UINT64_MAX, VK_NULL_HANDLE, swapChain->BackBufferAcquireFence, &swapChain->CurrentImageIndex));
    vkWaitForFences(this->graphicsDevice, 1, &swapChain->BackBufferAcquireFence, true, UINT64_MAX);
    vkResetFences(this->graphicsDevice, 1, &swapChain->BackBufferAcquireFence);
}

void* VulkanGraphicsService::CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length)
{
    VulkanQueryBuffer* queryBuffer = new VulkanQueryBuffer();
    queryBuffer->QueryBufferType = queryBufferType;
    queryBuffer->Length = length;

    VkQueryPoolCreateInfo createInfo = { VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO };
    createInfo.queryType = VK_QUERY_TYPE_TIMESTAMP;
    createInfo.queryCount = length;

    AssertIfFailed(vkCreateQueryPool(this->graphicsDevice, &createInfo, nullptr, &queryBuffer->QueryPool));

    return queryBuffer;
}

void VulkanGraphicsService::ResetQueryBuffer(void* queryBufferPointer)
{
    VulkanQueryBuffer* queryBuffer = (VulkanQueryBuffer*)queryBufferPointer;
    vkResetQueryPool(this->graphicsDevice, queryBuffer->QueryPool, 0, queryBuffer->Length);
}

void VulkanGraphicsService::SetQueryBufferLabel(void* queryBufferPointer, char* label){ }
void VulkanGraphicsService::DeleteQueryBuffer(void* queryBufferPointer)
{ 
    VulkanQueryBuffer* queryBuffer = (VulkanQueryBuffer*)queryBufferPointer;
    vkDestroyQueryPool(this->graphicsDevice, queryBuffer->QueryPool, nullptr);
}

void* VulkanGraphicsService::CreateShader(char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{
    VulkanShader* shader = new VulkanShader();

	auto currentDataPtr = (unsigned char*)shaderByteCode;

	// Skip SPIR-V offset
	auto spirvOffset = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);
	currentDataPtr += spirvOffset;

	auto shaderTableCount = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);

	for (int i = 0; i < shaderTableCount; i++)
	{
		auto entryPointNameLength = (*(int*)currentDataPtr);
		currentDataPtr += sizeof(int);

		auto entryPointNameTemp = new char[entryPointNameLength + 1];
		entryPointNameTemp[entryPointNameLength] = '\0';

		memcpy(entryPointNameTemp, (char*)currentDataPtr, entryPointNameLength);
		auto entryPointName = string(entryPointNameTemp);
		currentDataPtr += entryPointNameLength;

		auto shaderByteCodeLength = (*(int*)currentDataPtr);
		currentDataPtr += sizeof(int);

		auto shaderBlob = CreateShaderModule(this->graphicsDevice, currentDataPtr, shaderByteCodeLength);
		currentDataPtr += shaderByteCodeLength;

		if (entryPointName == "AmplificationMain")
		{
			shader->AmplificationShaderMethod = shaderBlob;
		}

		else if (entryPointName == "MeshMain")
		{
			shader->MeshShaderMethod = shaderBlob;
		}

		else if (entryPointName == "PixelMain")
		{
			shader->PixelShaderMethod = shaderBlob;
		}

		else if (entryPointName == string(computeShaderFunction))
		{
			shader->ComputeShaderMethod = shaderBlob;
		}
	}

    return shader;
}

void VulkanGraphicsService::SetShaderLabel(void* shaderPointer, char* label)
{ 
    #ifdef DEBUG 
    VulkanShader* shader = (VulkanShader*)shaderPointer;

    if (shader->AmplificationShaderMethod != nullptr)
    {
        VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
        nameInfo.objectType = VK_OBJECT_TYPE_SHADER_MODULE;
        nameInfo.objectHandle = (uint64_t)shader->AmplificationShaderMethod;
        nameInfo.pObjectName = label;

        AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    }

    if (shader->MeshShaderMethod != nullptr)
    {
        VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
        nameInfo.objectType = VK_OBJECT_TYPE_SHADER_MODULE;
        nameInfo.objectHandle = (uint64_t)shader->MeshShaderMethod;
        nameInfo.pObjectName = label;

        AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    }

    if (shader->PixelShaderMethod != nullptr)
    {
        VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
        nameInfo.objectType = VK_OBJECT_TYPE_SHADER_MODULE;
        nameInfo.objectHandle = (uint64_t)shader->PixelShaderMethod;
        nameInfo.pObjectName = label;

        AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    }

    if (shader->ComputeShaderMethod != nullptr)
    {
        VkDebugUtilsObjectNameInfoEXT nameInfo = { VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT };
        nameInfo.objectType = VK_OBJECT_TYPE_SHADER_MODULE;
        nameInfo.objectHandle = (uint64_t)shader->ComputeShaderMethod;
        nameInfo.pObjectName = label;

        AssertIfFailed(vkSetDebugUtilsObjectName(this->graphicsDevice, &nameInfo));
    }
    #endif
}

void VulkanGraphicsService::DeleteShader(void* shaderPointer)
{ 
    VulkanShader* shader = (VulkanShader*)shaderPointer;

    if (shader->AmplificationShaderMethod != nullptr)
    {
        vkDestroyShaderModule(this->graphicsDevice, shader->AmplificationShaderMethod, nullptr);
    }

    if (shader->MeshShaderMethod != nullptr)
    {
        vkDestroyShaderModule(this->graphicsDevice, shader->MeshShaderMethod, nullptr);
    }

    if (shader->PixelShaderMethod != nullptr)
    {
        vkDestroyShaderModule(this->graphicsDevice, shader->PixelShaderMethod, nullptr);
    }

    if (shader->ComputeShaderMethod != nullptr)
    {
        vkDestroyShaderModule(this->graphicsDevice, shader->ComputeShaderMethod, nullptr);
    }
}

void* VulkanGraphicsService::CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    VulkanShader* shader = (VulkanShader*)shaderPointer;
    VulkanPipelineState* pipelineState = new VulkanPipelineState();

    if (renderPassDescriptor.RenderTarget1TexturePointer.HasValue)
    {
        VulkanTexture* renderTargetTexture = (VulkanTexture*)renderPassDescriptor.RenderTarget1TexturePointer.Value;

        pipelineState->RenderPass = CreateRenderPass(this->graphicsDevice, renderPassDescriptor);
        pipelineState->PipelineLayoutObject = CreateGraphicsPipelineLayout(this->graphicsDevice, &pipelineState->DescriptorSetLayoutCount, &pipelineState->DescriptorSetLayouts);
        pipelineState->PipelineStateObject = CreateGraphicsPipeline(this->graphicsDevice, pipelineState->RenderPass, pipelineState->PipelineLayoutObject, renderPassDescriptor, shader);
    }

    return pipelineState;
}

void VulkanGraphicsService::SetPipelineStateLabel(void* pipelineStatePointer, char* label){ }

void VulkanGraphicsService::DeletePipelineState(void* pipelineStatePointer)
{ 
    VulkanPipelineState* pipelineState = (VulkanPipelineState*)pipelineStatePointer;

    vkDestroyRenderPass(this->graphicsDevice, pipelineState->RenderPass, nullptr);
    vkDestroyPipelineLayout(this->graphicsDevice, pipelineState->PipelineLayoutObject, nullptr);
    vkDestroyPipeline(this->graphicsDevice, pipelineState->PipelineStateObject, nullptr);

    delete pipelineState;
}

void VulkanGraphicsService::CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    VulkanGraphicsBuffer* destinationBuffer = (VulkanGraphicsBuffer*)destinationGraphicsBufferPointer;
    VulkanGraphicsBuffer* sourceBuffer = (VulkanGraphicsBuffer*)sourceGraphicsBufferPointer;
    
    VkBufferCopy copyRegion = {};
    copyRegion.size = sizeInBytes;

    vkCmdCopyBuffer(commandList->CommandBufferObject, sourceBuffer->BufferObject, destinationBuffer->BufferObject, 1, &copyRegion);
}

void VulkanGraphicsService::CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    VulkanTexture* destinationTexture = (VulkanTexture*)destinationTexturePointer;
    VulkanGraphicsBuffer* sourceBuffer = (VulkanGraphicsBuffer*)sourceGraphicsBufferPointer;
    
    VkBufferImageCopy copyRegion = {};
    copyRegion.imageExtent.width = width;
    copyRegion.imageExtent.height = height;
    copyRegion.imageExtent.depth = 1;
    copyRegion.imageSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
    copyRegion.imageSubresource.mipLevel = mipLevel;
    copyRegion.imageSubresource.layerCount = 1;

    // TODO: Fill other properties
    // TODO: Review the barrier mechanism
    TransitionTextureToState(commandList, destinationTexture, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, true);

    vkCmdCopyBufferToImage(commandList->CommandBufferObject, sourceBuffer->BufferObject, destinationTexture->TextureObject, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);
    
    TransitionTextureToState(commandList, destinationTexture, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, true);
}

void VulkanGraphicsService::CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer){ }

void VulkanGraphicsService::DispatchThreads(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ){ }

void VulkanGraphicsService::BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
	commandList->RenderPassDescriptor = renderPassDescriptor;

    if (renderPassDescriptor.RenderTarget1TexturePointer.HasValue == 1)
    {
        VulkanTexture* renderTargetTexture = (VulkanTexture*)renderPassDescriptor.RenderTarget1TexturePointer.Value;
        TransitionTextureToState(commandList, renderTargetTexture, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);

        uint32_t imageViewCount = 1;
        VkImageView imageViews[2] {};
        imageViews[0] = renderTargetTexture->ImageView;

        if (renderPassDescriptor.DepthTexturePointer.HasValue == 1)
        {
            VulkanTexture* depthTexture = (VulkanTexture*)renderPassDescriptor.DepthTexturePointer.Value;
            TransitionTextureToState(commandList, depthTexture, VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL);

            imageViews[1] = depthTexture->ImageView;
            imageViewCount++;
        }

        uint32_t clearColorCount = 0;
        VkClearValue clearColors[2] = {};
        
        if (renderPassDescriptor.RenderTarget1ClearColor.HasValue)
        {
            clearColorCount++;
            clearColors[0] = { renderPassDescriptor.RenderTarget1ClearColor.Value.X, renderPassDescriptor.RenderTarget1ClearColor.Value.Y, renderPassDescriptor.RenderTarget1ClearColor.Value.Z, 1 };
        }

        if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::ClearWrite)
        {
            clearColorCount++;
            clearColors[1] = { 0, 0, 0, 0 };
        }

        VkRenderPassBeginInfo passBeginInfo = { VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO };
        passBeginInfo.renderPass = this->currentPipelineState->RenderPass;

        // TODO: Can we avoid the frame buffer creation at each frame?
        commandList->RenderPassFrameBuffer = CreateFramebuffer(this->graphicsDevice, this->currentPipelineState->RenderPass, imageViews, imageViewCount, renderTargetTexture->Width, renderTargetTexture->Height);
        
        passBeginInfo.framebuffer = commandList->RenderPassFrameBuffer;
        passBeginInfo.renderArea.extent.width = renderTargetTexture->Width;
        passBeginInfo.renderArea.extent.height = renderTargetTexture->Height;
        passBeginInfo.clearValueCount = clearColorCount;
        passBeginInfo.pClearValues = clearColors;

        vkCmdBeginRenderPass(commandList->CommandBufferObject, &passBeginInfo, VK_SUBPASS_CONTENTS_INLINE);

        VkViewport viewport = { 0, (float)renderTargetTexture->Height, (float)renderTargetTexture->Width, -(float)renderTargetTexture->Height, 0, 1 };
        VkRect2D scissor = { {0, 0}, {renderTargetTexture->Width, renderTargetTexture->Height} };

        vkCmdSetViewport(commandList->CommandBufferObject, 0, 1, &viewport);
        vkCmdSetScissor(commandList->CommandBufferObject, 0, 1, &scissor);

        commandList->IsRenderPassActive = true;
    }
}

void VulkanGraphicsService::EndRenderPass(void* commandListPointer)
{ 
    // TODO: Find a way to delete the frame buffer that was created in the begin function
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    
    if (commandList->IsRenderPassActive)
    {
        // TODO: Refactor all of that
        vkCmdEndRenderPass(commandList->CommandBufferObject);
        commandList->IsRenderPassActive = false;
        frameBuffersToDelete.push_back(commandList->RenderPassFrameBuffer);
    }

    VulkanTexture* texture = (VulkanTexture*)commandList->RenderPassDescriptor.RenderTarget1TexturePointer.Value;

    if (texture->IsPresentTexture)
    {
        TransitionTextureToState(commandList, texture, VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);
    }

    else
    {
        TransitionTextureToState(commandList, texture, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }

    if (commandList->RenderPassDescriptor.DepthTexturePointer.HasValue == 1)
    {
        VulkanTexture* depthTexture = (VulkanTexture*)commandList->RenderPassDescriptor.DepthTexturePointer.Value;
        TransitionTextureToState(commandList, depthTexture, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }
}

void VulkanGraphicsService::SetPipelineState(void* commandListPointer, void* pipelineStatePointer)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    this->currentPipelineState = (VulkanPipelineState*)pipelineStatePointer;

    if (this->currentPipelineState->PipelineStateObject != nullptr)
    {
        // TODO: Support compute shaders
        vkCmdBindPipeline(commandList->CommandBufferObject, VK_PIPELINE_BIND_POINT_GRAPHICS, this->currentPipelineState->PipelineStateObject);
        vkCmdBindDescriptorSets(commandList->CommandBufferObject, VK_PIPELINE_BIND_POINT_GRAPHICS, this->currentPipelineState->PipelineLayoutObject, 0, 3, this->currentResourceHeap->DescriptorSets, 0, nullptr);
    }
}

void VulkanGraphicsService::SetShaderResourceHeap(void* commandListPointer, void* shaderResourceHeapPointer)
{ 
    VulkanShaderResourceHeap* resourceHeap = (VulkanShaderResourceHeap*)shaderResourceHeapPointer;
    this->currentResourceHeap = resourceHeap;
}

void VulkanGraphicsService::SetShader(void* commandListPointer, void* shaderPointer){ }
void VulkanGraphicsService::SetShaderParameterValues(void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;

    // TODO: Disable that
    if (commandList->IsRenderPassActive)
    {
        vkCmdPushConstants(commandList->CommandBufferObject, this->currentPipelineState->PipelineLayoutObject, VK_SHADER_STAGE_ALL, 0, valuesLength * 4, values);
    }
}

void VulkanGraphicsService::DispatchMesh(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;

    if (commandList->IsRenderPassActive)
    {
        vkCmdDrawMeshTasks(commandList->CommandBufferObject, threadGroupCountX, 0);
    }
}

void VulkanGraphicsService::BeginQuery(void* commandListPointer, void* queryBufferPointer, int index)
{ 
    
}

void VulkanGraphicsService::EndQuery(void* commandListPointer, void* queryBufferPointer, int index)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    VulkanQueryBuffer* queryBuffer = (VulkanQueryBuffer*)queryBufferPointer;

    if (queryBuffer->QueryBufferType == GraphicsQueryBufferType::Timestamp || queryBuffer->QueryBufferType == GraphicsQueryBufferType::CopyTimestamp)
    {
        vkCmdWriteTimestamp(commandList->CommandBufferObject, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, queryBuffer->QueryPool, index);
    }
}

void VulkanGraphicsService::ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    VulkanQueryBuffer* queryBuffer = (VulkanQueryBuffer*)queryBufferPointer;
    VulkanGraphicsBuffer* destinationBuffer = (VulkanGraphicsBuffer*)destinationBufferPointer;

    if (queryBuffer->QueryBufferType == GraphicsQueryBufferType::Timestamp)
    {
        vkCmdCopyQueryPoolResults(commandList->CommandBufferObject, queryBuffer->QueryPool, startIndex, endIndex - startIndex, destinationBuffer->BufferObject, 0, sizeof(uint64_t), VK_QUERY_RESULT_64_BIT | VK_QUERY_RESULT_WAIT_BIT);
    }
}

VkInstance VulkanGraphicsService::CreateVulkanInstance()
{
    VkInstance instance = {};

    VkApplicationInfo appInfo = { VK_STRUCTURE_TYPE_APPLICATION_INFO };
    appInfo.apiVersion = VK_API_VERSION_1_2;

    VkInstanceCreateInfo createInfo = { VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO };

    createInfo.pApplicationInfo = &appInfo;

#ifdef DEBUG
    const char* layers[] =
    {
        "VK_LAYER_KHRONOS_validation"
    };

    createInfo.ppEnabledLayerNames = layers;
	createInfo.enabledLayerCount = ARRAYSIZE(layers);
#endif

    const char* extensions[] =
    {
#ifdef DEBUG
		VK_EXT_DEBUG_REPORT_EXTENSION_NAME,
        VK_EXT_DEBUG_UTILS_EXTENSION_NAME,
#endif
        VK_KHR_SURFACE_EXTENSION_NAME,
        VK_KHR_WIN32_SURFACE_EXTENSION_NAME // TODO: Add a preprocessor check here
    };

    createInfo.ppEnabledExtensionNames = extensions;
	createInfo.enabledExtensionCount = ARRAYSIZE(extensions);

    AssertIfFailed(vkCreateInstance(&createInfo, nullptr, &instance));

    return instance;
}

VkPhysicalDevice VulkanGraphicsService::FindGraphicsDevice()
{
    uint32_t deviceCount = 16;
    VkPhysicalDevice devices[16];

    AssertIfFailed(vkEnumeratePhysicalDevices(this->vulkanInstance, &deviceCount, nullptr));
    AssertIfFailed(vkEnumeratePhysicalDevices(this->vulkanInstance, &deviceCount, devices));

    for (int i = 0; i < deviceCount; i++)
    {
        VkPhysicalDeviceProperties deviceProperties;
        vkGetPhysicalDeviceProperties(devices[i], &deviceProperties);

        VkPhysicalDeviceFeatures deviceFeatures;
        vkGetPhysicalDeviceFeatures(devices[i], &deviceFeatures);

        VkPhysicalDeviceMeshShaderFeaturesNV meshShaderFeatures = {};
        meshShaderFeatures.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MESH_SHADER_FEATURES_NV;

        VkPhysicalDeviceFeatures2 features2 = {};
        features2.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2_KHR;
        features2.pNext = &meshShaderFeatures;

        vkGetPhysicalDeviceFeatures2(devices[i], &features2);

        if (deviceProperties.deviceType == VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU && meshShaderFeatures.meshShader && meshShaderFeatures.taskShader)
        {
            char* currentDeviceName = deviceProperties.deviceName;
            this->deviceName = wstring(currentDeviceName, currentDeviceName + strlen(currentDeviceName));
            this->deviceName += wstring(L" (Vulkan " + to_wstring(VK_API_VERSION_MAJOR(VK_HEADER_VERSION_COMPLETE)) + L"." + to_wstring(VK_API_VERSION_MINOR(VK_HEADER_VERSION_COMPLETE)) + L"." + to_wstring(VK_API_VERSION_PATCH(VK_HEADER_VERSION_COMPLETE)) + L")");

            return devices[i];
        }
    }

    return 0;
}

VkDevice VulkanGraphicsService::CreateDevice(VkPhysicalDevice physicalDevice)
{
    VkDevice device;
    int test = 0;

    // TODO: Change that, queue creation is fixed
    uint32_t queueFamilyCount = 0;
    vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, nullptr);

    vector<VkQueueFamilyProperties> queueFamilies(queueFamilyCount);
    vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilies.data());

    VkDeviceQueueCreateInfo queueCreateInfos[3];

    for (int i = 0; i < queueFamilyCount; i++)
    {
        if (queueFamilies[i].queueFlags & VK_QUEUE_GRAPHICS_BIT)
        {
            this->renderCommandQueueFamilyIndex = i;
            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 2);
        }

        else if (queueFamilies[i].queueFlags & VK_QUEUE_COMPUTE_BIT)
        {
            this->computeCommandQueueFamilyIndex = i;
            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 1);
        }

        else if (queueFamilies[i].queueFlags & VK_QUEUE_TRANSFER_BIT)
        {
            this->copyCommandQueueFamilyIndex = i;
            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 1);
        }
    }

    VkPhysicalDeviceMemoryProperties deviceMemoryProperties;
    vkGetPhysicalDeviceMemoryProperties(this->graphicsPhysicalDevice, &deviceMemoryProperties);

    for (int i = 0; i < deviceMemoryProperties.memoryTypeCount; i++)
    {
        auto memoryPropertyFlags = deviceMemoryProperties.memoryTypes[i].propertyFlags;
        
        if ((memoryPropertyFlags & VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT) && (memoryPropertyFlags & VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT) == 0)
        {
            this->gpuMemoryTypeIndex = i;
        }

        else if ((memoryPropertyFlags & VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT) && (memoryPropertyFlags & VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT))
        {
            this->uploadMemoryTypeIndex = i;
        }

        else if ((memoryPropertyFlags & VK_MEMORY_PROPERTY_HOST_COHERENT_BIT) && (memoryPropertyFlags & VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT) && (memoryPropertyFlags & VK_MEMORY_PROPERTY_HOST_CACHED_BIT))
        {
            this->readBackMemoryTypeIndex = i;
        }
    }

    VkDeviceCreateInfo createInfo = { VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO };
    createInfo.queueCreateInfoCount = 3;
    createInfo.pQueueCreateInfos = queueCreateInfos;

    const char* extensions[] =
    {
        VK_KHR_TIMELINE_SEMAPHORE_EXTENSION_NAME,
        VK_KHR_SWAPCHAIN_EXTENSION_NAME,
        VK_KHR_SWAPCHAIN_MUTABLE_FORMAT_EXTENSION_NAME,
        VK_NV_MESH_SHADER_EXTENSION_NAME
    };

    createInfo.ppEnabledExtensionNames = extensions;
    createInfo.enabledExtensionCount = ARRAYSIZE(extensions);

    VkPhysicalDeviceMeshShaderFeaturesNV meshFeatures = { VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MESH_SHADER_FEATURES_NV };
    meshFeatures.meshShader = true;
    meshFeatures.taskShader = true;

    VkPhysicalDeviceVulkan12Features features = { VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES };
    features.timelineSemaphore = true;
    features.runtimeDescriptorArray = true;
    features.descriptorIndexing = true;
    features.descriptorBindingVariableDescriptorCount = true;
    features.descriptorBindingPartiallyBound = true;
    features.shaderSampledImageArrayNonUniformIndexing = true;
    features.separateDepthStencilLayouts = true;
    features.hostQueryReset = true;

    #ifdef DEBUG
    features.bufferDeviceAddressCaptureReplay = true;
    #endif
    features.pNext = &meshFeatures;

    createInfo.pNext = &features;

    AssertIfFailed(vkCreateDevice(physicalDevice, &createInfo, nullptr, &device));

    InitVulkanFeatureFunctions(this->vulkanInstance, device);
    
    return device;
}

static VkBool32 VKAPI_CALL DebugReportCallback(VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, uint64_t object, size_t location, int32_t messageCode, const char* pLayerPrefix, const char* pMessage, void* pUserData)
{
	// This silences warnings like "For optimal performance image layout should be VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL instead of GENERAL."
	// We'll assume other performance warnings are also not useful.
	// if (flags & VK_DEBUG_REPORT_PERFORMANCE_WARNING_BIT_EXT)
    // {
	// 	return VK_FALSE;
    // }

	const char* type = "[93mVULKAN INFO";

	if (flags & VK_DEBUG_REPORT_ERROR_BIT_EXT)
    {
        type = "[91mVULKAN ERROR";
    }

    else if (flags & VK_DEBUG_REPORT_WARNING_BIT_EXT)
    {
	    type = "[93mVULKAN WARNING";
    }

	char message[4096];
	snprintf(message, 4096, "%s: %s\n[0m", type, pMessage);

	printf("%s", message);

#ifdef _WIN32
	OutputDebugStringA(message);
#endif

	if (flags & VK_DEBUG_REPORT_ERROR_BIT_EXT)
    {
		// assert(!"Vulkan validation error encountered!");
    }

	return VK_FALSE;
}

void VulkanGraphicsService::RegisterDebugCallback()
{
	VkDebugReportCallbackCreateInfoEXT createInfo = { VK_STRUCTURE_TYPE_DEBUG_REPORT_CREATE_INFO_EXT };
	createInfo.flags = VK_DEBUG_REPORT_WARNING_BIT_EXT | VK_DEBUG_REPORT_PERFORMANCE_WARNING_BIT_EXT | VK_DEBUG_REPORT_ERROR_BIT_EXT;
	createInfo.pfnCallback = DebugReportCallback;

	AssertIfFailed(vkCreateDebugReportCallback(this->vulkanInstance, &createInfo, 0, &this->debugCallback));
}