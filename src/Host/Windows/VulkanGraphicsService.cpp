#pragma once
#include "WindowsCommon.h"
#include "VulkanGraphicsService.h"
#include "VulkanGraphicsServiceUtils.h"

VulkanGraphicsService::VulkanGraphicsService()
{
    this->vulkanInstance = CreateVulkanInstance();

    this->graphicsPhysicalDevice = FindGraphicsDevice();
    this->graphicsDevice = CreateDevice(this->graphicsPhysicalDevice);
}

VulkanGraphicsService::~VulkanGraphicsService()
{
    if (this->graphicsDevice != nullptr)
    {
        vkDestroyDevice(this->graphicsDevice, nullptr);
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

GraphicsAllocationInfos VulkanGraphicsService::GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    GraphicsAllocationInfos result = {};
	result.SizeInBytes = 1024 * 1024;
	result.Alignment = 64;

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

    vkGetDeviceQueue(this->graphicsDevice, queueFamilyIndex, 0, &commandQueue->CommandQueueObject);

    auto commandPools = new VkCommandPool[VulkanFramesCount];

	// Init command allocators for each frame in flight
	// TODO: For multi threading support we need to allocate on allocator per frame per thread
	for (int i = 0; i < VulkanFramesCount; i++)
	{
        VkCommandPoolCreateInfo createInfo = { VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO };
        createInfo.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT ;
        createInfo.queueFamilyIndex = queueFamilyIndex;

        VkCommandPool commandPool = 0;
        AssertIfFailed(vkCreateCommandPool(this->graphicsDevice, &createInfo, 0, &commandPool));

		commandPools[i] = commandPool;
	}

    commandQueue->CommandPools = commandPools;

    VkSemaphoreTypeCreateInfo timelineCreateInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_TYPE_CREATE_INFO };
    timelineCreateInfo.semaphoreType = VK_SEMAPHORE_TYPE_TIMELINE;
    timelineCreateInfo.initialValue = 0;

    VkSemaphoreCreateInfo createInfo = { VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO };
    createInfo.pNext = &timelineCreateInfo;

    AssertIfFailed(vkCreateSemaphore(this->graphicsDevice, &createInfo, NULL, &commandQueue->TimelineSemaphore));

    return commandQueue;
}

void VulkanGraphicsService::SetCommandQueueLabel(void* commandQueuePointer, char* label){ }
void VulkanGraphicsService::DeleteCommandQueue(void* commandQueuePointer){ }

void VulkanGraphicsService::ResetCommandQueue(void* commandQueuePointer)
{
    VulkanCommandQueue* commandQueue = (VulkanCommandQueue*)commandQueuePointer;

    auto commandPool = commandQueue->CommandPools[this->currentCommandPoolIndex];
    AssertIfFailed(vkResetCommandPool(this->graphicsDevice, commandPool, 0));
}

unsigned long VulkanGraphicsService::GetCommandQueueTimestampFrequency(void* commandQueuePointer)
{
    return 1000;
}

unsigned long VulkanGraphicsService::ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength)
{
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

void VulkanGraphicsService::SetCommandListLabel(void* commandListPointer, char* label){ }
void VulkanGraphicsService::DeleteCommandList(void* commandListPointer){ }

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
    return new VulkanGraphicsHeap();
}

void VulkanGraphicsService::SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label){ }
void VulkanGraphicsService::DeleteGraphicsHeap(void* graphicsHeapPointer){ }

void* VulkanGraphicsService::CreateShaderResourceHeap(unsigned long length)
{
    return new VulkanShaderResourceHeap();
}

void VulkanGraphicsService::SetShaderResourceHeapLabel(void* shaderResourceHeapPointer, char* label){ }
void VulkanGraphicsService::DeleteShaderResourceHeap(void* shaderResourceHeapPointer){ }
void VulkanGraphicsService::CreateShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index, void* texturePointer){ }
void VulkanGraphicsService::DeleteShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index){ }
void VulkanGraphicsService::CreateShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer){ }
void VulkanGraphicsService::DeleteShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index){ }

void* VulkanGraphicsService::CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{
    VulkanGraphicsBuffer* graphicsBuffer = new VulkanGraphicsBuffer();
    graphicsBuffer->SizeInBytes = sizeInBytes;
    return graphicsBuffer;
}

void VulkanGraphicsService::SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label){ }
void VulkanGraphicsService::DeleteGraphicsBuffer(void* graphicsBufferPointer){ }

void* VulkanGraphicsService::GetGraphicsBufferCpuPointer(void* graphicsBufferPointer)
{
    VulkanGraphicsBuffer* graphicsBuffer = (VulkanGraphicsBuffer*)graphicsBufferPointer;
    return new unsigned char[graphicsBuffer->SizeInBytes];
}

void* VulkanGraphicsService::CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    return new VulkanTexture();
}

void VulkanGraphicsService::SetTextureLabel(void* texturePointer, char* label){ }
void VulkanGraphicsService::DeleteTexture(void* texturePointer){ }

void* VulkanGraphicsService::CreateSwapChain(void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
    VulkanSwapChain* swapChain = new VulkanSwapChain();
    swapChain->CommandQueue = (VulkanCommandQueue*)commandQueuePointer;

    VkWin32SurfaceCreateInfoKHR surfaceCreateInfo = { VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR };
    surfaceCreateInfo.hinstance = GetModuleHandle(nullptr);
    surfaceCreateInfo.hwnd = (HWND)windowPointer;

    AssertIfFailed(vkCreateWin32SurfaceKHR(this->vulkanInstance, &surfaceCreateInfo, nullptr, &swapChain->WindowSurface));

    // TODO: Check with the real queue index
    VkBool32 isPresentSupported;
    AssertIfFailed(vkGetPhysicalDeviceSurfaceSupportKHR(this->graphicsPhysicalDevice, 0, swapChain->WindowSurface, &isPresentSupported));
    assert(isPresentSupported == 1);

    VkSwapchainCreateInfoKHR swapChainCreateInfo = { VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR };
    swapChainCreateInfo.surface = swapChain->WindowSurface;
    swapChainCreateInfo.minImageCount = VulkanFramesCount;
    swapChainCreateInfo.imageFormat = VulkanConvertTextureFormat(textureFormat, true);
    swapChainCreateInfo.imageColorSpace = VK_COLOR_SPACE_SRGB_NONLINEAR_KHR;
    swapChainCreateInfo.imageExtent.width = width;
    swapChainCreateInfo.imageExtent.height = height;
    swapChainCreateInfo.imageArrayLayers = 1;
    swapChainCreateInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    swapChainCreateInfo.imageSharingMode = VK_SHARING_MODE_EXCLUSIVE; // TODO: Check that and check if we must set the queue families
    swapChainCreateInfo.presentMode = VK_PRESENT_MODE_FIFO_KHR;
    swapChainCreateInfo.preTransform = VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR;
    swapChainCreateInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;

    AssertIfFailed(vkCreateSwapchainKHR(this->graphicsDevice, &swapChainCreateInfo, nullptr, &swapChain->SwapChainObject));

	uint32_t swapchainImageCount = VulkanFramesCount;
	VkImage swapchainImages[VulkanFramesCount];
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, nullptr));
	AssertIfFailed(vkGetSwapchainImagesKHR(this->graphicsDevice, swapChain->SwapChainObject, &swapchainImageCount, swapchainImages));

    for (int i = 0; i < swapchainImageCount; i++)
    {
        VulkanTexture* backBufferTexture = new VulkanTexture();
        backBufferTexture->TextureObject = swapchainImages[i];
        backBufferTexture->IsPresentTexture = true;
        backBufferTexture->ImageView = CreateImageView(this->graphicsDevice, swapchainImages[i], textureFormat);
        backBufferTexture->Width = width;
        backBufferTexture->Height = height;

        swapChain->BackBufferTextures[i] = backBufferTexture;
    }

    return swapChain;
}

void VulkanGraphicsService::ResizeSwapChain(void* swapChainPointer, int width, int height)
{

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
    VulkanSwapChain* swapChain = (VulkanSwapChain*)swapChainPointer;

    // TODO: To replace with a present wait on the TimelineSemaphore or better?
    AssertIfFailed(vkDeviceWaitIdle(this->graphicsDevice));
	AssertIfFailed(vkAcquireNextImageKHR(this->graphicsDevice, swapChain->SwapChainObject, ~0ull, VK_NULL_HANDLE, VK_NULL_HANDLE, &swapChain->CurrentImageIndex));
}

void* VulkanGraphicsService::CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length)
{
    return new VulkanQueryBuffer();
}

void VulkanGraphicsService::SetQueryBufferLabel(void* queryBufferPointer, char* label){ }
void VulkanGraphicsService::DeleteQueryBuffer(void* queryBufferPointer){ }

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

void VulkanGraphicsService::SetShaderLabel(void* shaderPointer, char* label){ }
void VulkanGraphicsService::DeleteShader(void* shaderPointer){ }

void* VulkanGraphicsService::CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    VulkanPipelineState* pipelineState = new VulkanPipelineState();

    // TODO: Handle the proper amount of attachments
    VkAttachmentDescription attachments[1] = {};

    if (renderPassDescriptor.RenderTarget1TexturePointer.HasValue)
    {
        VulkanTexture* renderTargetTexture = (VulkanTexture*)renderPassDescriptor.RenderTarget1TexturePointer.Value;

        if (renderTargetTexture->IsPresentTexture)
        {
            // TODO: Handle RT operations defined in the renderPassDescriptor
            attachments[0].format = VulkanConvertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value, true);
            attachments[0].samples = VK_SAMPLE_COUNT_1_BIT;
            attachments[0].loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
            attachments[0].storeOp = VK_ATTACHMENT_STORE_OP_STORE;
            attachments[0].stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
            attachments[0].stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
            attachments[0].initialLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
            attachments[0].finalLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

            VkAttachmentReference colorAttachments = { 0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL };

            VkSubpassDescription subpass = {};
            subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
            subpass.colorAttachmentCount = 1;
            subpass.pColorAttachments = &colorAttachments;

            VkRenderPassCreateInfo createInfo = { VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO };
            createInfo.attachmentCount = 1;
            createInfo.pAttachments = attachments;
            createInfo.subpassCount = 1;
            createInfo.pSubpasses = &subpass;

            AssertIfFailed(vkCreateRenderPass(this->graphicsDevice, &createInfo, 0, &pipelineState->RenderPass));
        }
    }

    return pipelineState;
}

void VulkanGraphicsService::SetPipelineStateLabel(void* pipelineStatePointer, char* label){ }
void VulkanGraphicsService::DeletePipelineState(void* pipelineStatePointer){ }

void VulkanGraphicsService::CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes){ }
void VulkanGraphicsService::CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel){ }
void VulkanGraphicsService::CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer){ }

void VulkanGraphicsService::DispatchThreads(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ){ }

void VulkanGraphicsService::BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{ 
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;

    if (renderPassDescriptor.RenderTarget1TexturePointer.HasValue == 1)
    {
        VulkanTexture* renderTargetTexture = (VulkanTexture*)renderPassDescriptor.RenderTarget1TexturePointer.Value;

        if (renderTargetTexture->IsPresentTexture)
        {
            VkClearColorValue color = { 0.5f, 1.0f, 0.25f, 1 };
            VkClearValue clearColor = { color };

            VkRenderPassBeginInfo passBeginInfo = { VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO };
            passBeginInfo.renderPass = this->currentPipelineState->RenderPass;

            // TODO: Can we avoid the frame buffer creation at each frame?
            passBeginInfo.framebuffer = CreateFramebuffer(this->graphicsDevice, this->currentPipelineState->RenderPass, renderTargetTexture->ImageView, renderTargetTexture->Width, renderTargetTexture->Height);
            
            passBeginInfo.renderArea.extent.width = renderTargetTexture->Width;
            passBeginInfo.renderArea.extent.height = renderTargetTexture->Height;
            passBeginInfo.clearValueCount = 1;
            passBeginInfo.pClearValues = &clearColor;

            vkCmdBeginRenderPass(commandList->CommandBufferObject, &passBeginInfo, VK_SUBPASS_CONTENTS_INLINE);

            VkViewport viewport = { 0, 0, (float)renderTargetTexture->Width, (float)renderTargetTexture->Height, 0, 1 };
            VkRect2D scissor = { {0, 0}, {renderTargetTexture->Width, renderTargetTexture->Height} };

            vkCmdSetViewport(commandList->CommandBufferObject, 0, 1, &viewport);
            vkCmdSetScissor(commandList->CommandBufferObject, 0, 1, &scissor);

            commandList->IsRenderPassActive = true;
        }
    }
}

void VulkanGraphicsService::EndRenderPass(void* commandListPointer)
{ 
    // TODO: Find a way to delete the frame buffer that was created in the begin function
    VulkanCommandList* commandList = (VulkanCommandList*)commandListPointer;
    
    if (commandList->IsRenderPassActive)
    {
        vkCmdEndRenderPass(commandList->CommandBufferObject);
        commandList->IsRenderPassActive = false;
    }
}

void VulkanGraphicsService::SetPipelineState(void* commandListPointer, void* pipelineStatePointer)
{ 
    this->currentPipelineState = (VulkanPipelineState*)pipelineStatePointer;
}

void VulkanGraphicsService::SetShaderResourceHeap(void* commandListPointer, void* shaderResourceHeapPointer){ }
void VulkanGraphicsService::SetShader(void* commandListPointer, void* shaderPointer){ }
void VulkanGraphicsService::SetShaderParameterValues(void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength){ }

void VulkanGraphicsService::DispatchMesh(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ){ }

void VulkanGraphicsService::BeginQuery(void* commandListPointer, void* queryBufferPointer, int index){ }
void VulkanGraphicsService::EndQuery(void* commandListPointer, void* queryBufferPointer, int index){ }
void VulkanGraphicsService::ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex){ }

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
	createInfo.enabledLayerCount = 1;
#endif

    const char* extensions[] =
    {
        VK_KHR_SURFACE_EXTENSION_NAME,
        VK_KHR_WIN32_SURFACE_EXTENSION_NAME // TODO: Add a preprocessor check here
    };

    createInfo.ppEnabledExtensionNames = extensions;
	createInfo.enabledExtensionCount = 2;

    AssertIfFailed(vkCreateInstance(&createInfo, nullptr, &instance));

    return instance;
}

VkPhysicalDevice VulkanGraphicsService::FindGraphicsDevice()
{
    uint32_t deviceCount = 16;
    VkPhysicalDevice devices[16];

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
    // TODO: Enable mesh shaders
    // https://fluffels.github.io/vulkanMesh/

    VkDevice device;
    int test = 0;

    // TODO: Change that, queue creation is fixed
    uint32_t queueFamilyCount = 16;
    VkQueueFamilyProperties queueFamilies[16];
    VkDeviceQueueCreateInfo queueCreateInfos[3];

    vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilies);

    for (int i = 0; i < queueFamilyCount; i++)
    {
        if (queueFamilies[i].queueFlags & VK_QUEUE_GRAPHICS_BIT)
        {
            this->renderCommandQueueFamilyIndex = i;

            float queuePriorities[]
            {
                1.0f, 1.0f
            };

            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 2);
        }

        else if (queueFamilies[i].queueFlags & VK_QUEUE_COMPUTE_BIT)
        {
            this->computeCommandQueueFamilyIndex = i;

            float queuePriorities[]
            {
                1.0f
            };

            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 1);
        }

        else if (queueFamilies[i].queueFlags & VK_QUEUE_TRANSFER_BIT)
        {
            this->copyCommandQueueFamilyIndex = i;

            float queuePriorities[]
            {
                1.0f
            };

            queueCreateInfos[i] = CreateDeviceQueueCreateInfo(i, 1);
        }
    }

    VkDeviceCreateInfo createInfo = { VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO };
    createInfo.queueCreateInfoCount = 3;
    createInfo.pQueueCreateInfos = queueCreateInfos;

    const char* extensions[] =
    {
        VK_KHR_TIMELINE_SEMAPHORE_EXTENSION_NAME,
        VK_KHR_SWAPCHAIN_EXTENSION_NAME
    };

    createInfo.ppEnabledExtensionNames = extensions;
    createInfo.enabledExtensionCount = 2;

    VkPhysicalDeviceVulkan12Features features = { VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES };
    features.timelineSemaphore = true;

    createInfo.pNext = &features;

    AssertIfFailed(vkCreateDevice(physicalDevice, &createInfo, nullptr, &device));

    return device;
}