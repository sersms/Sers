################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Sers/Core/Module/Log/Logger.cpp 

OBJS += \
./Sers/Core/Module/Log/Logger.o 

CPP_DEPS += \
./Sers/Core/Module/Log/Logger.d 


# Each subdirectory must supply rules for building sources it contributes
Sers/Core/Module/Log/%.o: ../Sers/Core/Module/Log/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -O0 -g3 -Wall -c -fmessage-length=0 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


